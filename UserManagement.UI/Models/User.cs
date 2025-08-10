using System.ComponentModel.DataAnnotations;
using UserManagement.UI.Helpers;

namespace UserManagement.UI.Models;

public class User
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Forename is required.")]
    [StringLength(50, ErrorMessage = "Forename cannot exceed 50 characters.")]
    public string Forename { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surname is required.")]
    [StringLength(50, ErrorMessage = "Surname cannot exceed 50 characters.")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Active status is required.")]
    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Date of Birth is required.")]
    [PastDate]
    public DateTime? DateOfBirth { get; set; }
}
