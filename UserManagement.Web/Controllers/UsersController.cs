using System;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Users;

namespace UserManagement.WebMS.Controllers;

[Route("users")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "isActive")] bool? isActive)
    {
        try
        {
            var users = await _userService.FilterByActive(isActive);

            var items = users.Select(p => new UserListItemViewModel
            {
                Id = p.Id,
                Forename = p.Forename,
                Surname = p.Surname,
                Email = p.Email,
                IsActive = p.IsActive,
                DateOfBirth = p.DateOfBirth
            });

            var model = new UserListViewModel
            {
                Items = items.ToList()
            };

            return View(model);

        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while retrieving the user list.");
        }
    }
}
