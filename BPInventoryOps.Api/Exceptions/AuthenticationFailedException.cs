namespace BPInventoryOps.Api.Exceptions;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException() : base("Authentication failed.")
    {
    }
}
