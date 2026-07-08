using Onion.Common.Models;

namespace Onion.Common.Exceptions
{
    public class CustomException : Exception
    {
        public Error Error { get; }

        public CustomException(Error error) : base(error.Message)
        {
            Error = error;
        }
    }
}