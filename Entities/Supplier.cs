using System.ComponentModel.DataAnnotations;

namespace PDVNow.Entities;

public sealed class Supplier
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TradeName { get; set; }

    [MaxLength(14)]
    public string? Cnpj { get; set; }

    [MaxLength(20)]
    public string? StateRegistration { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? AddressLine1 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(10)]
    public string? PostalCode { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
