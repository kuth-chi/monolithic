using Monolithic.Api.Modules.Platform.Notifications.Domain;

namespace Monolithic.Api.Modules.Platform.Notifications.Contracts;

public sealed record SendNotificationRequest(
    NotificationChannel Channel,
    string Recipient,
    string TemplateSl,
    Dictionary<string, object?> Variables,
    Guid? BusinessId = null,
    Guid? UserId = null,
    string? SubjectOverride = null,
    string? BodyOverride = null
);

public sealed record NotificationLogDto(
    Guid Id,
    Guid? BusinessId,
    Guid? UserId,
    NotificationChannel Channel,
    string TemplateSl,
    string Recipient,
    string? Subject,
    string Body,
    NotificationStatus Status,
    string? ErrorMessage,
    int AttemptCount,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset CreatedAtUtc
);

public sealed record NotificationListRequest(
    Guid? BusinessId = null,
    Guid? UserId = null,
    NotificationChannel? Channel = null,
    NotificationStatus? Status = null,
    int Page = 1,
    int PageSize = 20
);
