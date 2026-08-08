namespace Application.Common.Exceptions;

public sealed class BootstrapConflictException(string message) : Exception(message);
