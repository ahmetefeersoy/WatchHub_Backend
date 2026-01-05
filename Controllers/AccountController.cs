using System;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Interfaces;
using api.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    /// <summary>
    /// Legacy Account Controller - Authentication endpoints have been moved to SupabaseAuthController
    /// This controller now only handles profile management operations
    /// </summary>
    [Route("api/account")]
    [ApiController]
    [Obsolete("Authentication endpoints are deprecated. Use /api/auth endpoints (SupabaseAuthController) instead.")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
        }

        /// <summary>
        /// [DEPRECATED] Use /api/auth/login instead
        /// </summary>
        [HttpPost("login")]
        [Obsolete("This endpoint is deprecated. Use /api/auth/login (SupabaseAuthController) instead.")]
        public IActionResult Login()
        {
            return BadRequest(new { 
                message = "This endpoint is deprecated. Please use /api/auth/login for Supabase authentication.",
                newEndpoint = "/api/auth/login"
            });
        }

        /// <summary>
        /// [DEPRECATED] Use /api/auth/register instead
        /// </summary>
        [HttpPost("register")]
        [Obsolete("This endpoint is deprecated. Use /api/auth/register (SupabaseAuthController) instead.")]
        public IActionResult Register()
        {
            return BadRequest(new { 
                message = "This endpoint is deprecated. Please use /api/auth/register for Supabase authentication.",
                newEndpoint = "/api/auth/register"
            });
        }

        /// <summary>
        /// [DEPRECATED] Two-factor authentication is now handled by Supabase
        /// </summary>
        [HttpPost("enable-two-factor")]
        [Obsolete("Two-factor authentication is now handled by Supabase.")]
        public IActionResult EnableTwoFactor()
        {
            return StatusCode(503, new { message = "Two-factor authentication is now handled by Supabase. Please enable MFA in your Supabase account settings." });
        }

        /// <summary>
        /// [DEPRECATED] Two-factor authentication is now handled by Supabase
        /// </summary>
        [HttpPost("disable-two-factor")]
        [Obsolete("Two-factor authentication is now handled by Supabase.")]
        public IActionResult DisableTwoFactor()
        {
            return StatusCode(503, new { message = "Two-factor authentication is now handled by Supabase. Please disable MFA in your Supabase account settings." });
        }

        /// <summary>
        /// [DEPRECATED] Email verification is now handled automatically by Supabase
        /// </summary>
        [HttpPost("verify-two-factor")]
        [Obsolete("Email verification is now handled automatically by Supabase.")]
        public IActionResult VerifyTwoFactor()
        {
            return StatusCode(503, new { message = "Email verification is now handled automatically by Supabase." });
        }
[HttpGet("user/{username}")]
[Authorize]
public async Task<IActionResult> GetUser(string username)
{
    var user = await _userManager.Users
                .Where(x => x.UserName == username.ToLower())
                .Select(u => new
                {
                    u.UserName,
                    u.Email,
                    FirstName = u.FirstName ?? string.Empty, // Null kontrolü
                    LastName = u.LastName ?? string.Empty,   // Null kontrolü
                    Country = u.Country ?? string.Empty     // Null kontrolü
                })
                .FirstOrDefaultAsync();

    if (user == null) return NotFound("User not found");

    return Ok(user);
}



        [HttpPost("update-user-profile")]
[Authorize]
public async Task<IActionResult> UpdateUserProfile([FromForm] UpdateUserDto updateUserProfileDto)
{
    if (string.IsNullOrEmpty(updateUserProfileDto.Username))
    {
        return BadRequest("Username is required");
    }
    
    var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == updateUserProfileDto.Username.ToLower());
    if (user == null) return Unauthorized("User not found");

    // Null gelen değerler mevcut değerlerin üzerine yazılmamalıdır.
    if (!string.IsNullOrEmpty(updateUserProfileDto.FirstName))
    {
        user.FirstName = updateUserProfileDto.FirstName;
    }

    if (!string.IsNullOrEmpty(updateUserProfileDto.LastName))
    {
        user.LastName = updateUserProfileDto.LastName;
    }

    if (!string.IsNullOrEmpty(updateUserProfileDto.Country))
    {
        user.Country = updateUserProfileDto.Country;
    }

    var updateResult = await _userManager.UpdateAsync(user);

    if (!updateResult.Succeeded)
    {
        return StatusCode(500, updateResult.Errors);
    }

    return Ok(new
    {
        user.UserName,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Country
    });
}

        [HttpPost("report-and-contact")]
        public async Task<IActionResult> ReportAndContact([FromBody] ReportAndContactDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Contact form email sending is disabled - email service moved to Supabase
            // Store contact messages in database or use a separate service
            return StatusCode(501, "Contact form submission is temporarily unavailable. Please email directly to iletisimwatchhub@gmail.com");
        }


        [HttpDelete("delete/{username}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string username)
        {
            // Get the authenticated user's username from the token
            var authenticatedUsername = User.Identity?.Name;
            
            // Ensure the user can only delete their own account
            if (authenticatedUsername == null || !authenticatedUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized("You can only delete your own account");
            }

            var user = await _userManager.FindByNameAsync(username.ToLower());
            if (user == null) return NotFound("User not found");

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok("User account deleted successfully");
            }
            else
            {
                return StatusCode(500, result.Errors);
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByNameAsync(changePasswordDto.Username.ToLower());
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Mevcut şifreyi kontrol et
            var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, changePasswordDto.CurrentPassword, false);
            if (!checkPasswordResult.Succeeded)
            {
                return Unauthorized("Current password is incorrect");
            }

            // Yeni şifreyi güncelle
            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("Password changed successfully");
        }


        // Email verification is handled by Supabase - no SendEmailAsync needed

    }
}