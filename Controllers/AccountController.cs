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
    [Route("api/account")]
    [ApiController]
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

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == loginDto.Username.ToLower());

            if (user == null) return Unauthorized("Invalid Username");

            if (string.IsNullOrEmpty(user.Email))
            {
                return Unauthorized("Email alanı boş olamaz.");
            }

            if (!user.EmailConfirmed)
            {
                return Unauthorized("Please confirm your email address before logging in.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized("Username not found and/or password incorrect");
            }

            // Kullanıcının TwoFactorEnabled olup olmadığını kontrol et
            if (user.TwoFactorEnabled)
            {
                // 2FA is not supported with legacy auth - use Supabase Auth for MFA
                return BadRequest("Two-factor authentication is only supported via Supabase Auth. Please use the new authentication system.");
            }

            return Ok(
                new NewUserDto
                {
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Token = _tokenService.CreateToken(user)
                }
            );
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // E-posta kontrolü
            var emailExists = await _userManager.Users.AnyAsync(u => u.Email == registerDto.Email);
            if (emailExists)
            {
                return BadRequest("Bu e-posta adresi zaten kullanılıyor.");
            }

            var appUser = new AppUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password ?? string.Empty);

            if (createdUser.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                if (roleResult.Succeeded)
                {
                    return Ok(new NewUserDto
                    {
                        Username = appUser.UserName ?? string.Empty,
                        Email = appUser.Email ?? string.Empty,
                        Token = _tokenService.CreateToken(appUser)
                    });
                }
                else
                {
                    return StatusCode(500, roleResult.Errors);
                }
            }
            else
            {
                return StatusCode(500, createdUser.Errors);
            }
        }

        [HttpPost("enable-two-factor")]
        public async Task<IActionResult> EnableTwoFactor(string username)
        {
            // Two-factor authentication is temporarily disabled
            return StatusCode(503, "Two-factor authentication is temporarily unavailable.");
        }

        [HttpPost("disable-two-factor")]
        public async Task<IActionResult> DisableTwoFactor(string username)
        {
            // Two-factor authentication is temporarily disabled
            return StatusCode(503, "Two-factor authentication is temporarily unavailable.");
        }

        // Email verification is handled by Supabase automatically
        // No manual confirmation endpoint needed

        [HttpPost("verify-two-factor")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto dto)
        {
            // Two-factor authentication is temporarily disabled
            return StatusCode(503, "Two-factor authentication is temporarily unavailable.");
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