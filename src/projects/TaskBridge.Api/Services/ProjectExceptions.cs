namespace TaskBridge.Api.Services;

public sealed class AuthenticationRequiredException(string message) : Exception(message);

public sealed class ForbiddenOperationException(string message) : Exception(message);

public sealed class ResourceNotFoundException(string message) : Exception(message);

public sealed class ConcurrencyConflictException(string message) : Exception(message);