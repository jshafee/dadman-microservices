public sealed record TokenExchangeRequest(string ExternalSubject, Guid TenantId, Guid ApplicationId);
public sealed record TokenExchangeResponse(string AccessToken, int ExpiresInSeconds);
