namespace PDVNow.Entities;

public sealed class CashSessionReopenEvent
{
    public Guid Id { get; set; }

    public Guid CashSessionId { get; set; }

    public Guid ReopenedByAdminUserId { get; set; }

    public DateTimeOffset ReopenedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Justification { get; set; } = string.Empty;

    public Guid? AdminOverrideCodeId { get; set; }
}
