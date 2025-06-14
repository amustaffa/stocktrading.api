using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StockTrading.Models.Domain;
using StockTrading.Models.DTO;
using StockTrading.Configurations;
using Microsoft.Extensions.Logging;
using StockTrading.Service.Interfaces;

namespace StockTrading.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto model)
        {
            _logger.LogInformation("Attempting to register new user: {Email}", model.Email);
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: User with email {Email} already exists.", model.Email);
                throw new ApplicationException("User with this email already exists.");
            }

            var user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email, // Use email as username for simplicity
                EmailConfirmed = true // For simplicity, auto-confirm email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} registered successfully. Generating JWT.", user.Email);
                var token = GenerateJwtToken(user);
                return new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email!,
                    UserId = user.Id,
                    Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
                };
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("User registration failed for {Email}: {Errors}", model.Email, errors);
            throw new ApplicationException($"Failed to register user: {errors}");
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto model)
        {
            _logger.LogInformation("Attempting to login user: {Email}", model.Email);
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found.", model.Email);
                throw new ApplicationException("Invalid credentials.");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for user {Email}.", model.Email);
                throw new ApplicationException("Invalid credentials.");
            }

            _logger.LogInformation("User {Email} logged in successfully. Generating JWT.", user.Email);
            var token = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                UserId = user.Id,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
            };
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}