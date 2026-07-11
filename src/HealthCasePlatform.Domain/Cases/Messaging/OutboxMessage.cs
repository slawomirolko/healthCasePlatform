using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases.Messaging;

public sealed class OutboxMessage : Entity
{
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; internal set; }
    public int Attempts { get; internal set; }
    public string? LastError { get; internal set; }

    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    internal static OutboxMessage Create(string type, string payload) => new()
    {
        Id = Guid.CreateVersion7(),
        Type = type,
        Payload = payload,
        OccurredAtUtc = DateTime.UtcNow,
        Attempts = 0
    };
}
