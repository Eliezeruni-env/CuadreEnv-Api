using Onion.Domain;

namespace Onion.Domain.Users;

public class UserCompany : BaseEntity
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string Role { get; set; } = "Employee";
    public bool IsDefault { get; set; }
}
