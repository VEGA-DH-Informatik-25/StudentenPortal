using CampusConnect.Domain.Entities;

namespace CampusConnect.Application.Common.Interfaces;

public interface IFeedAttachmentStorage
{
    Task<FeedAttachment> SaveAsync(Stream content, string originalFileName, string contentType, long sizeBytes, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(FeedAttachment attachment, CancellationToken cancellationToken = default);
    Task DeleteAsync(FeedAttachment attachment, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<FeedAttachment> attachments, CancellationToken cancellationToken = default);
}
