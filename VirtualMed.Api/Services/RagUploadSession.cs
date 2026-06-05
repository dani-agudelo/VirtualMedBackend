using VirtualMed.Application.Interfaces;

namespace VirtualMed.Api.Services;

public sealed class RagUploadSession : IRagUploadSession
{
    private string? _path;
    private long _sizeBytes;

    public void SetTempFile(string path, long sizeBytes)
    {
        _path = path;
        _sizeBytes = sizeBytes;
    }

    public (string Path, long SizeBytes) TakeTempFile()
    {
        if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
            throw new InvalidOperationException("No hay archivo PDF en la sesion de subida.");

        var path = _path;
        var size = _sizeBytes;
        _path = null;
        _sizeBytes = 0;
        return (path, size);
    }
}
