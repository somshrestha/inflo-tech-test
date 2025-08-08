using System;

namespace UserManagement.Web.UserHelpers;

public class UserValidator : IUserValidator
{
    public bool IsValidDateOfBirth(DateTime? dateOfBirth) =>
        !dateOfBirth.HasValue || dateOfBirth.Value <= DateTime.Today;
}
