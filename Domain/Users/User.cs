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

        [Required]
        public string Identification { get; set; }

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
        public int CompanyId { get; set; }
        // Simple role string to support basic RBAC (e.g. "Admin", "Employee")
        public string Role { get; set; } = "Employee";
    }
}
