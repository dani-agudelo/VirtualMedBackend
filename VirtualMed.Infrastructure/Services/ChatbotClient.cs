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

    public ChatbotClient(HttpClient httpClient, IOptions<ChatbotSettings> settings)
    {
        _httpClient = httpClient;
        var baseUrl = settings.Value.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.Value.TimeoutSeconds);
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
}
