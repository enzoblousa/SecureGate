namespace SecureGate.Api.Controllers.Contracts;

public sealed record ValidationErrorResponse(IReadOnlyList<string> Errors);
