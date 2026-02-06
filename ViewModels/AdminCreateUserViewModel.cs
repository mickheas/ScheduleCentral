using System.ComponentModel.DataAnnotations;

namespace ScheduleCentral.Models.ViewModels
{
    public class AdminCreateUserViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Select at least one role.")]
        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; } = new();
    }
}
