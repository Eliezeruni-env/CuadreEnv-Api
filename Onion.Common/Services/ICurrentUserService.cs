using System;

namespace Onion.Common.Services
{
    public interface ICurrentUserService
    {
        int? CompanyId { get; }
        int? UserId { get; }
        bool IsAuthenticated { get; }
    }
}
