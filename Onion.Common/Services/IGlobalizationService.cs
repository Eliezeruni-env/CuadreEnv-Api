using Onion.Common.Models;
using Onion.Common.Enums;

namespace Onion.Common.Services
{
    public interface IGlobalizationService
    {
        Error GetErrorInCurrentLanguage(ErrorCodes code);
    }
}
