using System.ComponentModel.DataAnnotations;

namespace UserManagement.UI.Helpers;

public class PastDateAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date && date > DateTime.Now)
        {
            return new ValidationResult("Date of birth cannot be in the future.");
        }
        return ValidationResult.Success!;
    }
}
