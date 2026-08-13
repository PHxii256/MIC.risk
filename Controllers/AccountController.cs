using MIC.risk.Data;
using MIC.risk.DTOs.Auth;
using MIC.risk.Interfaces;
using MIC.risk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MIC.risk.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDBContext _context;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        ApplicationDBContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NewUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var appUser = await _userManager.FindByEmailAsync(dto.Email);
        if (appUser == null)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(appUser, dto.Password, false);
        if (!signInResult.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentityUserId == appUser.Id);

        if (employee == null)
        {
            return Unauthorized(new { Message = "No employee profile linked to this account." });
        }

        if (!employee.Active)
        {
            return Unauthorized(new { Message = "Your employee account is inactive." });
        }

        var roles = await _userManager.GetRolesAsync(appUser);

        return Ok(new NewUserDto
        {
            UserName = appUser.UserName ?? string.Empty,
            Email = appUser.Email ?? string.Empty,
            Token = _tokenService.CreateToken(appUser, roles)
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return Unauthorized(new { Message = "User is not authenticated." });
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentityUserId == appUser.Id);

        if (employee == null || !employee.Active)
        {
            return Unauthorized(new { Message = "Your employee account is inactive." });
        }

        var result = await _userManager.ChangePasswordAsync(appUser, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = message });
        }

        return NoContent();
    }
}
