using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.AuditLogs;
using UserManagement.Web.Models.Users;
using UserManagement.Web.UserHelpers;

namespace UserManagement.WebMS.Controllers;

[Route("users")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly IUserValidator _userValidator;
    public UsersController(IUserService userService, IMapper mapper, IUserValidator userValidator)
    {
        _userService = userService;
        _mapper = mapper;
        _userValidator = userValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "isActive")] bool? isActive)
    {
        try
        {
            var users = await _userService.FilterByActive(isActive);
            var model = new UserListViewModel
            {
                Items = users.Select(_mapper.Map<UserListItemViewModel>).ToList()
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
            return View(model);

        try
        {
            if (!_userValidator.IsValidDateOfBirth(model.DateOfBirth))
            {
                ModelState.AddModelError("DateOfBirth", "Date of Birth cannot be in the future.");
                return View(model);
            }

            var user = _mapper.Map<User>(model);
            await _userService.CreateAsync(user);

            return RedirectToAction(nameof(List));
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> View(long id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var auditLogs = await _userService.GetUserAuditLogs(id);
            var model = new UserWithAuditViewModel
            {
                User = _mapper.Map<UserViewModel>(user),
                AuditLogs = auditLogs.Select(_mapper.Map<AuditLogViewModel>).ToList()
            };

            return View(model);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(long id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = _mapper.Map<UserViewModel>(user);

            return View(model);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }

    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, UserViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (!_userValidator.IsValidDateOfBirth(model.DateOfBirth))
            {
                ModelState.AddModelError("DateOfBirth", "Date of Birth cannot be in the future.");
                return View(model);
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            _mapper.Map(model, user);
            await _userService.UpdateAsync(user);

            return RedirectToAction(nameof(List));
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "An error occurred while updating the user. Please try again.");
            return View(model);
        }
    }

    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = _mapper.Map<UserViewModel>(user);

            return View(model);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while deleting the user.");
        }
    }

    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userService.DeleteAsync(user);
            return RedirectToAction(nameof(List));
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while deleting the user.");
        }
    }
}
