namespace Onion.Common.Exceptions;

public sealed class FiscalSequenceExhaustedException : Exception
{
    public FiscalSequenceExhaustedException(string message) : base(message) { }
}

public sealed class FiscalSequenceExpiredException : Exception
{
    public FiscalSequenceExpiredException(string message) : base(message) { }
}

public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException(string message) : base(message) { }
}

public sealed class CashSessionException : Exception
{
    public string Code { get; }
    public CashSessionException(string code, string message) : base(message) => Code = code;
}
