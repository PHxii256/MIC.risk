using MIC.risk.DTOs.Auth;
using MIC.risk.Interfaces;
using MIC.risk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MIC.risk.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
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

        var roles = await _userManager.GetRolesAsync(appUser);

        return Ok(new NewUserDto
        {
            UserName = appUser.UserName ?? string.Empty,
            Email = appUser.Email ?? string.Empty,
            Token = _tokenService.CreateToken(appUser, roles)
        });
    }
}
