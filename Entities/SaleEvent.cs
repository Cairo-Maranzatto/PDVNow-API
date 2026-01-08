namespace PDVNow.Entities;

public sealed class SaleEvent
{
    public Guid Id { get; set; }

    public Guid SaleId { get; set; }

    public SaleEventType Type { get; set; }

    public string? Details { get; set; }

    public Guid PerformedByUserId { get; set; }

    public DateTimeOffset PerformedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
