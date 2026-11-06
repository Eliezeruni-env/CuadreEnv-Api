using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Onion.Domain.Users
{
    public class User : BaseEntity
    {
        //User info
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        // Identification is optional during onboarding; make nullable to allow users without it
        public string? Identification { get; set; }

        public string Gender { get; set; } = "M";

        //Login infor
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        // Simple role string to support basic RBAC (e.g. "Admin", "Employee")
        public string Role { get; set; } = "Employee";

        // Last successful login timestamp (nullable)
        public DateTime? LastLoginAt { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = "República Dominicana";
        public string? OperatingLocation { get; set; }
        public string? IpAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? LastLoginIp { get; set; }
        public string? AllowedModulesJson { get; set; }
        // Allow marking a user as super-user (persistent override for management tools)
        public bool IsSuperUser { get; set; } = false;
        public bool CanAccessMobilePOS { get; set; } = true;
        public bool CanOpenCashDrawer { get; set; } = false;
        public bool CanConfigureFiscalReceipt { get; set; } = true;
        public bool CanChangeThemeAndLanguage { get; set; } = true;
    }
}
