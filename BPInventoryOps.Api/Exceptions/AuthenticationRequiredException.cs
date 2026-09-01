namespace BPInventoryOps.Api.Exceptions;

public sealed class AuthenticationRequiredException(
    string message = "Authentication is required.") : Exception(message);
