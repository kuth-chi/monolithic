namespace Monolithic.Api.Common.Storage;

/// <summary>
/// Abstraction for storing and retrieving image files.
/// Implementations can target local disk, Azure Blob, S3, etc.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Persist an uploaded file and return the relative storage path.
    /// </summary>
    /// <param name="file">The uploaded form file.</param>
    /// <param name="folder">Logical sub-folder (e.g., "inventory/{itemId}").</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Relative path stored in the database (e.g., "inventory/abc123/xyz.jpg").</returns>
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a previously saved file by its relative storage path.
    /// </summary>
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert a relative storage path to a publicly accessible URL or path.
    /// </summary>
    string GetPublicUrl(string storagePath);
}
