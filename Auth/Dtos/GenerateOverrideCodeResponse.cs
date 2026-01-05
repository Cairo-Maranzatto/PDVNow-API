namespace PDVNow.Auth.Dtos;

public sealed record GenerateOverrideCodeResponse(
    string Code,
    DateTimeOffset ExpiresAtUtc);
