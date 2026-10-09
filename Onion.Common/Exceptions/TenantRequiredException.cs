namespace Onion.Common.Exceptions;

public sealed class TenantRequiredException : Exception
{
    public TenantRequiredException(string message = "A valid company is required.") : base(message)
    {
    }
}
