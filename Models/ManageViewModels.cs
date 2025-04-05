using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;

namespace Secure_Student_Management_System.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; } = new List<UserLoginInfo>();
        public string? PhoneNumber { get; set; } // Nullable
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }

        // Add a list of external logins (e.g., Google)
        public IList<ExternalLoginViewModel> ExternalLogins { get; set; } = new List<ExternalLoginViewModel>();
    }

    public class ExternalLoginViewModel
    {
        public string LoginProvider { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
        public string? ProviderDisplayName { get; set; } // Google display name or other providers
    }



    public class RemovePhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty; // Default empty string
    }

    public class ManageLoginsViewModel
    {


        public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();
        public IList<AuthenticationScheme> OtherLogins { get; set; } = new List<AuthenticationScheme>();
        public bool ShowRemoveButton { get; set; }
        // Add a list of external logins for managing them, like Google
        public IList<ExternalLoginViewModel> ExternalLogins { get; set; } = new List<ExternalLoginViewModel>();
    }


    public class FactorViewModel
    {
        public string? Purpose { get; set; } // Nullable
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Number { get; set; } = string.Empty;
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ConfigureTwoFactorViewModel
    {
        public string? SelectedProvider { get; set; } // Nullable
        public ICollection<SelectListItem> Providers { get; set; } = new List<SelectListItem>();
    }
}
