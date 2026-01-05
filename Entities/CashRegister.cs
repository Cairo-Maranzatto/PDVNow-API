using System.ComponentModel.DataAnnotations;

namespace PDVNow.Entities;

public sealed class CashRegister
{
    public Guid Id { get; set; }

    public int Code { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
