using System.Collections.Concurrent;
using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Infrastructure.Configuration;

namespace VirtualMed.Infrastructure.Services;

public class MinioService : IMinioService
{
    private readonly Lazy<IMinioClient> _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioService> _logger;
    private readonly ConcurrentDictionary<string, byte> _verifiedBuckets = new();

    public MinioService(
        IOptions<MinioSettings> settings,
        ILogger<MinioService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            throw new InvalidOperationException("La configuración de MinIO 'Endpoint' no puede estar vacía. Verifica el archivo appsettings.json.");

        if (string.IsNullOrWhiteSpace(_settings.AccessKey))
            throw new InvalidOperationException("La configuración de MinIO 'AccessKey' no puede estar vacía. Verifica el archivo appsettings.json.");

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new InvalidOperationException("La configuración de MinIO 'SecretKey' no puede estar vacía. Verifica el archivo appsettings.json.");

        _minioClient = new Lazy<IMinioClient>(() =>
        {
            _logger.LogInformation(
                "Inicializando cliente MinIO: {Endpoint}, SSL: {UseSsl}",
                _settings.Endpoint,
                _settings.UseSsl);

            return new MinioClient()
                .WithEndpoint(_settings.Endpoint)
                .WithCredentials(_settings.AccessKey, _settings.SecretKey)
                .WithSSL(_settings.UseSsl)
                .Build();
        });
    }

    public async Task UploadAsync(string bucket, string objectName, Stream data, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureBucketExistsAsync(bucket, cancellationToken);

            if (data.CanSeek)
                data.Position = 0;

            var objectSize = GetReadableByteCount(data);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(objectSize)
                .WithContentType("application/pdf");

            await _minioClient.Value.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Archivo subido exitosamente: {Bucket}/{ObjectName} ({SizeBytes} bytes)",
                bucket,
                objectName,
                objectSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al subir archivo a MinIO: {Bucket}/{ObjectName}",
                bucket,
                objectName);
            throw;
        }
    }

    public async Task DeleteAsync(string bucket, string objectName, CancellationToken cancellationToken)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await _minioClient.Value.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Archivo eliminado de MinIO: {Bucket}/{ObjectName}",
                bucket,
                objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al eliminar archivo de MinIO: {Bucket}/{ObjectName}",
                bucket,
                objectName);
            throw;
        }
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        if (_verifiedBuckets.ContainsKey(bucketName))
            return;

        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            bool exists = await _minioClient.Value.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!exists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await _minioClient.Value.MakeBucketAsync(makeBucketArgs, cancellationToken);

                _logger.LogInformation("Bucket creado: {BucketName}", bucketName);
            }

            _verifiedBuckets.TryAdd(bucketName, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar/crear bucket: {BucketName}", bucketName);
            throw;
        }
    }

    private static long GetReadableByteCount(Stream data)
    {
        if (!data.CanSeek)
            throw new InvalidOperationException("El stream debe ser seekable para calcular el tamaño del objeto.");

        return data.Length - data.Position;
    }
}
