namespace VirtualMed.Application.Interfaces;

/// <summary>
/// Archivo temporal scoped por request para evitar pasar buffers grandes por MediatR.
/// </summary>
public interface IRagUploadSession
{
    void SetTempFile(string path, long sizeBytes);

    (string Path, long SizeBytes) TakeTempFile();
}
