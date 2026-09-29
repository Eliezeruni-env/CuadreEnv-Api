using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Onion.BussinesLogic.Dtos
{
    public record UserListDto(int Id, string FullName, string FirstName, string LastName, string Email, string UserName, string Role, int? CompanyId, string? CompanyName, bool Active, DateTime? LastLoginAt, bool IsSuperUser, string? Address = null, string? City = null, string? Country = null, string? OperatingLocation = null, string? IpAddress = null, decimal? Latitude = null, decimal? Longitude = null, string? LastLoginIp = null);

    public class UserDetailDto
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Identification { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool Active { get; set; }
        public DateTime CreationDate { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? ModificationDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? LastLoginAt { get; set; }
        // If server generated a temporary password and it was not emailed, it may be returned here (admin use only)
        public string? TemporaryPassword { get; set; }
        // Indicates whether a temporary password was emailed to the user
        public bool TempPasswordSent { get; set; }
        public bool IsSuperUser { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = "República Dominicana";
        public string? OperatingLocation { get; set; }
        public string? IpAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? LastLoginIp { get; set; }
    }

    public class CreateUserRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        public string? Identification { get; set; }
        public string? PhoneNumber { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Role { get; set; } = "Employee";
        public int? CompanyId { get; set; }
        public string? Password { get; set; }
        public string? TemporaryPassword { get; set; }
        public bool SendByEmail { get; set; } = true;
        public bool Active { get; set; } = true;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? OperatingLocation { get; set; }
        public string? IpAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    public class UpdateUserRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Identification { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; } = "M";
        public string Role { get; set; } = "Employee";
        public int? CompanyId { get; set; }
        public bool Active { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? OperatingLocation { get; set; }
        public string? IpAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    public class ResetPasswordRequest
    {
        public bool SendByEmail { get; set; } = true;
        public string? TemporaryPassword { get; set; }
    }

    public class UserStatusToggleRequest
    {
        public bool Active { get; set; }
    }

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ChangePasswordRequest
    {
        // For self-change: current password required. For admin reset, currentPassword may be null.
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
