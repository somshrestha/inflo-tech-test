﻿using System;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Models;
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

    [HttpGet("add")]
    public IActionResult Add()
    {
        return View(new UserViewModel());
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(UserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            if (model.DateOfBirth.HasValue && model.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError("DateOfBirth", "Date of Birth cannot be in the future.");
                return View(model);
            }

            var user = new User
            {
                Id = model.Id,
                Forename = model.Forename,
                Surname = model.Surname,
                Email = model.Email,
                IsActive = model.IsActive,
                DateOfBirth = model.DateOfBirth
            };

            await _userService.CreateAsync(user);
            return RedirectToAction(nameof(List));
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }
}
