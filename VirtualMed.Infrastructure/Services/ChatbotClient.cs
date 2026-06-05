using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces.Services;

namespace VirtualMed.Infrastructure.Services;

public class ChatbotClient : IChatbotClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ChatbotSettings _settings;

    public ChatbotClient(HttpClient httpClient, IOptions<ChatbotSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        var baseUrl = _settings.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_settings.InternalApiKey)
            && !_httpClient.DefaultRequestHeaders.Contains("X-Internal-Api-Key"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", _settings.InternalApiKey);
        }
    }

    public async Task<ChatbotHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("health", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new ChatbotHealthStatus { Status = "unavailable" };

            var body = await response.Content.ReadFromJsonAsync<HealthResponseDto>(JsonOptions, cancellationToken);
            return new ChatbotHealthStatus { Status = body?.Status ?? "unavailable" };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalServiceException(
                "No se pudo contactar el asistente clínico.",
                "Chatbot");
        }
    }

    public async Task<ChatbotApiResult> SendMessageAsync(
        string sessionId,
        string message,
        int similarityTopK,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "chat",
                new ChatRequestDto
                {
                    SessionId = sessionId,
                    Message = message,
                    SimilarityTopK = similarityTopK
                },
                JsonOptions,
                cancellationToken);
        }
        catch (TaskCanceledException)
        {
            throw new ExternalServiceException(
                "El asistente tardó demasiado en responder.",
                "Chatbot");
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException(
                "No se pudo contactar el asistente clínico.",
                ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ExternalServiceException(
                $"El asistente respondió con error ({(int)response.StatusCode}): {body}",
                "Chatbot");
        }

        var result = await response.Content.ReadFromJsonAsync<ChatResponseDto>(JsonOptions, cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Answer))
            throw new ExternalServiceException("Respuesta vacía del asistente clínico.", "Chatbot");

        return new ChatbotApiResult
        {
            Answer = result.Answer,
            Sources = result.Sources?.Select(s => new ChatbotSourceItem
            {
                FileName = s.FileName,
                PageLabel = s.PageLabel,
                Score = s.Score
            }).ToList() ?? []
        };
    }

    public async Task<ChatbotIngestResult> IngestDocumentAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(pdfStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(streamContent, "file", fileName);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.IngestTimeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("ingest/upload", content, cts.Token);
        }
        catch (TaskCanceledException)
        {
            throw new ExternalServiceException(
                "La indexación del documento tardó demasiado.",
                "Chatbot");
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException(
                "No se pudo contactar el servicio de indexación.",
                ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new BusinessRuleException("RAG_DUPLICATE", ExtractDetail(body));

        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException(
                $"El servicio de indexación respondió con error ({(int)response.StatusCode}): {ExtractDetail(body)}",
                "Chatbot");

        var result = JsonSerializer.Deserialize<IngestUploadResponseDto>(body, JsonOptions);
        if (result is null)
            throw new ExternalServiceException("Respuesta vacía del servicio de indexación.", "Chatbot");

        return new ChatbotIngestResult
        {
            FileName = result.FileName,
            IndexedDocuments = result.IndexedDocuments,
            IndexedNodes = result.IndexedNodes
        };
    }

    public async Task<IReadOnlyList<ChatbotIndexedDocument>> ListIndexedDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("ingest/documents", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        var items = await response.Content.ReadFromJsonAsync<List<RagDocumentItemDto>>(JsonOptions, cancellationToken);
        return items?.Select(i => new ChatbotIndexedDocument
        {
            FileName = i.FileName,
            IndexedNodes = i.IndexedNodes,
            FileSizeBytes = i.FileSizeBytes
        }).ToList() ?? [];
    }

    public async Task DeleteIndexedDocumentAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(fileName);
        var response = await _httpClient.DeleteAsync($"ingest/documents/{encoded}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ExternalServiceException(
                $"No se pudo eliminar el documento del índice ({(int)response.StatusCode}): {ExtractDetail(body)}",
                "Chatbot");
        }
    }

    private static string ExtractDetail(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.ValueKind switch
                {
                    JsonValueKind.String => detail.GetString() ?? body,
                    JsonValueKind.Array => string.Join("; ", detail.EnumerateArray().Select(e => e.GetRawText())),
                    _ => detail.GetRawText()
                };
            }
        }
        catch
        {
            // ignore parse errors
        }

        return string.IsNullOrWhiteSpace(body) ? "sin detalle" : body;
    }

    private sealed class HealthResponseDto
    {
        public string Status { get; set; } = "";
    }

    private sealed class ChatRequestDto
    {
        public required string SessionId { get; init; }
        public required string Message { get; init; }
        public required int SimilarityTopK { get; init; }
    }

    private sealed class ChatResponseDto
    {
        public string Answer { get; set; } = "";
        public List<SourceItemDto>? Sources { get; set; }
    }

    private sealed class SourceItemDto
    {
        public string FileName { get; set; } = "";
        public string PageLabel { get; set; } = "";
        public double? Score { get; set; }
    }

    private sealed class IngestUploadResponseDto
    {
        public string FileName { get; set; } = "";
        public int IndexedDocuments { get; set; }
        public int IndexedNodes { get; set; }
    }

    private sealed class RagDocumentItemDto
    {
        public string FileName { get; set; } = "";
        public int IndexedNodes { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
