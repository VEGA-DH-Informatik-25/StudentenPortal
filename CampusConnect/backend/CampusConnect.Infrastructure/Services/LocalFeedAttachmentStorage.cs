using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Domain.Entities;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CampusConnect.Infrastructure.Services;

public sealed class LocalFeedAttachmentStorage(
    IOptions<FeedAttachmentStorageOptions> options,
    IHostEnvironment environment) : IFeedAttachmentStorage
{
    private readonly FileExtensionContentTypeProvider _contentTypes = new();

    public async Task<FeedAttachment> SaveAsync(Stream content, string originalFileName, string contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(UploadRoot);

        var safeOriginalName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginalName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(UploadRoot, storedFileName);

        await using (var output = File.Create(path))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(contentType) || contentType == "application/octet-stream")
        {
            _contentTypes.TryGetContentType(safeOriginalName, out var detectedContentType);
            contentType = detectedContentType ?? string.Empty;
        }

        return new FeedAttachment
        {
            OriginalFileName = safeOriginalName,
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = sizeBytes,
            IsImage = IsImageExtension(extension)
        };
    }

    public Task<Stream?> OpenReadAsync(FeedAttachment attachment, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(UploadRoot, attachment.StoredFileName);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public Task DeleteAsync(FeedAttachment attachment, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(UploadRoot, attachment.StoredFileName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    public async Task DeleteManyAsync(IEnumerable<FeedAttachment> attachments, CancellationToken cancellationToken = default)
    {
        foreach (var attachment in attachments)
            await DeleteAsync(attachment, cancellationToken);
    }

    private string UploadRoot
    {
        get
        {
            var configuredPath = options.Value.UploadPath;
            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        }
    }

    private static bool IsImageExtension(string extension) => extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
}
