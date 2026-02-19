namespace Monolithic.Api.Common.Storage;

/// <summary>
/// Stores images on the local file system under <c>wwwroot/images/</c>.
/// Suitable for development and single-server deployments.
/// Replace with a cloud-backed implementation (Azure Blob / S3) for production
/// by registering a different <see cref="IImageStorageService"/> in DI.
/// </summary>
public sealed class LocalImageStorageService : IImageStorageService
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly string _storageRoot;
    private readonly string _baseUrl;

    public LocalImageStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
        _storageRoot = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "images");
        Directory.CreateDirectory(_storageRoot);

        var request = httpContextAccessor.HttpContext?.Request;
        _baseUrl = request is not null
            ? $"{request.Scheme}://{request.Host}/images"
            : "/images";
    }

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("File is empty.", nameof(file));

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"File exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.");

        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new InvalidOperationException($"Content type '{file.ContentType}' is not permitted. Allowed: {string.Join(", ", AllowedContentTypes)}");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(folder, uniqueName).Replace('\\', '/');
        var absoluteDir = Path.Combine(_storageRoot, folder);

        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, uniqueName);
        await using var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(stream, cancellationToken);

        return relativePath;
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return Task.FromResult(false);

        var absolutePath = Path.Combine(_storageRoot, storagePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(absolutePath))
            return Task.FromResult(false);

        File.Delete(absolutePath);
        return Task.FromResult(true);
    }

    public string GetPublicUrl(string storagePath) =>
        string.IsNullOrWhiteSpace(storagePath)
            ? string.Empty
            : $"{_baseUrl}/{storagePath.TrimStart('/')}";
}
