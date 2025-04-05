using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Secure_Student_Management_System.Models
{
    // Enum for enrollment status
    public enum EnrolmentStatus
    {
        Active,
        Inactive,
        Graduated,
        Dropped
    }

    public class Student
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;  // Default to active

        [Required]
        public EnrolmentStatus EnrolmentStatus { get; set; } = EnrolmentStatus.Inactive; // Default status: Inactive

        public string? ProfileImageUrl { get; set; } // Azure Blob Storage URL, nullable

        public void Activate()
        {
            EnrolmentStatus = EnrolmentStatus.Active;
            IsActive = true;
        }

        public void Deactivate()
        {
            EnrolmentStatus = EnrolmentStatus.Inactive;
            IsActive = false;
        }

        public void Graduate()
        {
            EnrolmentStatus = EnrolmentStatus.Graduated;
            IsActive = false; // Graduated students are considered inactive
        }

        public void DropOut()
        {
            EnrolmentStatus = EnrolmentStatus.Dropped;
            IsActive = false; // Dropped students are considered inactive
        }
    }

    public class StudentIndexViewModel
    {
        public List<Student> Students { get; set; } = new List<Student>();  // Ensure it's initialized
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchQuery { get; set; } // Nullable
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Admin"; // Default role is Admin

        public void AssignRole(string role)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                Role = role;
            }
        }

        public bool IsAdmin()
        {
            return Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
