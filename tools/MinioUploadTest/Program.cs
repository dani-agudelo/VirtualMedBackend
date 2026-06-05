using Minio;
using Minio.DataModel.Args;

const int sizeBytes = 17_404_204;
Console.WriteLine($"Creando buffer de {sizeBytes} bytes...");
var data = new byte[sizeBytes];
Random.Shared.NextBytes(data);

Console.WriteLine("Inicializando cliente MinIO...");
var client = new MinioClient()
    .WithEndpoint("localhost:9000")
    .WithCredentials("virtualmed", "VirtualMed123")
    .WithSSL(false)
    .Build();

await using var stream = new MemoryStream(data, writable: false);
var put = new PutObjectArgs()
    .WithBucket("rag-documents")
    .WithObject($"test-upload-{Guid.NewGuid():N}.pdf")
    .WithStreamData(stream)
    .WithObjectSize(data.Length)
    .WithContentType("application/pdf");

Console.WriteLine("Subiendo a MinIO...");
await client.PutObjectAsync(put);
Console.WriteLine("Upload OK");
