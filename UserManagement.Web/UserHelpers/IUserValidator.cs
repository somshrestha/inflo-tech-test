using System;

namespace UserManagement.Web.UserHelpers;

public interface IUserValidator
{
    bool IsValidDateOfBirth(DateTime? dateOfBirth);
}
