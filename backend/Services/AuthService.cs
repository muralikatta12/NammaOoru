using Microsoft.IdentityModel.Tokens;
using NammaOoru.Data;
using NammaOoru.Entities;
using NammaOoru.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace NammaOoru.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task<OtpResponse> SendOtpAsync(SendOtpRequest request);
        Task<LoginResponse> VerifyOtpAsync(VerifyOtpRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        string GenerateJwtToken(User user);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IOtpService otpService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _otpService = otpService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email and password are required."
                    };
                }

                // Check if user already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "User with this email already exists."
                    };
                }

                // Create new user
                var user = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PasswordHash = HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User registered successfully: {request.Email}");

                return new LoginResponse
                {
                    Success = true,
                    Message = "User registered successfully. Please verify your email.",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsEmailVerified = user.IsEmailVerified,
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                return new LoginResponse
                {
                    Success = false,
                    Message = $"An error occurred during registration: {ex.Message}"
                };
            }
        }

        public async Task<OtpResponse> SendOtpAsync(SendOtpRequest request)
{
    var normalizedEmail = request.Email.Trim().ToLower();

    // Delete old OTPs
    var oldOtps = _context.OtpVerifications.Where(o => o.Email == normalizedEmail);
    _context.OtpVerifications.RemoveRange(oldOtps);

    var otp = new Random().Next(100000, 999999).ToString("D6");

    var otpRecord = new OtpVerification
    {
        Email = normalizedEmail,
        OtpCode = otp,
        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
        IsUsed = false
    };

    _context.OtpVerifications.Add(otpRecord);
    await _context.SaveChangesAsync();

    // FOR TESTING — REMOVE IN PRODUCTION
    _logger.LogInformation($"OTP for {normalizedEmail}: {otp}");

    // TODO: Send real email here
    // await _emailService.SendEmailAsync(normalizedEmail, "Your NammaOoru OTP", $"Your OTP is: {otp}");

    return new OtpResponse { Success = true, Message = "OTP sent to your email" };
}
        public async Task<LoginResponse> VerifyOtpAsync(VerifyOtpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email and OTP code are required."
                    };
                }

                if (!_otpService.ValidateOtpLength(request.OtpCode))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid OTP format."
                    };
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                // Find valid OTP by email (not UserId, since OTP is created before user exists)
                var otp = await _context.OtpVerifications
                    .Where(o => o.Email == request.Email && 
                                o.OtpCode == request.OtpCode && 
                                !o.IsUsed && 
                                o.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid or expired OTP."
                    };
                }

                // Mark OTP as used and link it to the user
                otp.IsUsed = true;
                otp.UserId = user.Id;  // Link OTP to user now that they're verified
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;

                _context.OtpVerifications.Update(otp);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Send welcome email in background (don't wait for it)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(request.Email, user.FirstName);
                        _logger.LogInformation($"Welcome email sent to {request.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", request.Email);
                    }
                });

                var token = GenerateJwtToken(user);

                _logger.LogInformation($"OTP verified for {request.Email}");

                return new LoginResponse
                {
                    Success = true,
                    Message = "Email verified successfully.",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsEmailVerified = user.IsEmailVerified,
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying OTP: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                return new LoginResponse
                {
                    Success = false,
                    Message = $"An error occurred while verifying OTP: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email and password are required."
                    };
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                if (!user.IsEmailVerified)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email not verified. Please verify your email first."
                    };
                }

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                var token = GenerateJwtToken(user);

                _logger.LogInformation($"User logged in: {request.Email}");

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsEmailVerified = user.IsEmailVerified,
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                return new LoginResponse
                {
                    Success = false,
                    Message = $"An error occurred during login: {ex.Message}"
                };
            }
        }

        public string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            // Use the user's persisted role, default to "Citizen" if not set
            var role = user.Role ?? "Citizen";

            var claims = new[]
            {
                new System.Security.Claims.Claim("id", user.Id.ToString()),
                new System.Security.Claims.Claim("email", user.Email),
                new System.Security.Claims.Claim("firstName", user.FirstName),
                new System.Security.Claims.Claim("lastName", user.LastName),
                // Include both the ClaimTypes.Role and a plain "role" claim to be compatible
                // with various claim mappings and frontend checks.
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role),
                new System.Security.Claims.Claim("role", role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _configuration["Jwt:Issuer"] ?? "MoodlyAPI",
                Audience = _configuration["Jwt:Audience"] ?? "MoodlyAppUsers",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }
    }
}
