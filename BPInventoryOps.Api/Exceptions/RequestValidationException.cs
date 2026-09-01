namespace BPInventoryOps.Api.Exceptions;

public sealed class RequestValidationException(string message) : Exception(message);
