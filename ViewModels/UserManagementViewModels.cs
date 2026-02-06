using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScheduleCentral.Models.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public string RolesDisplay => Roles != null && Roles.Any() ? string.Join(", ", Roles) : "None";
        public bool IsActive { get; set; } // Maps to IsApproved
    }

    public class AdminManageUserViewModel
    {
        [Required]
        public string Id { get; set; }

        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; } = new();

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; }

        public bool IsAdminUser { get; set; }

        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        // Roles available for selection in the Manage User modal
        public List<SelectListItem> Roles { get; set; } = new();
    }
}