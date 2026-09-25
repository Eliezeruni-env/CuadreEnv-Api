using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Onion.Domain.Users
{
    public class User : BaseEntity
    {
        //User info
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        // Identification is optional during onboarding; make nullable to allow users without it
        public string? Identification { get; set; }

        [Required]
        [MaxLength(1)]
        public string Gender { get; set; }
            

        //Login infor
        [EmailAddress]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public string UserName { get; set; }
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
        // Allow marking a user as super-user (persistent override for management tools)
        public bool IsSuperUser { get; set; } = false;
    }
}
