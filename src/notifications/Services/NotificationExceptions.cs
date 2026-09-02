namespace TaskBridge.Notifications.Services;

public sealed class ForbiddenOperationException(string message) : Exception(message);
public sealed class ResourceNotFoundException(string message) : Exception(message);
public sealed class ConflictOperationException(string message) : Exception(message);