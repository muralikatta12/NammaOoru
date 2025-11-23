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
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return new OtpResponse
                    {
                        Success = false,
                        Message = "Email is required."
                    };
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return new OtpResponse
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                // Generate OTP
                var otp = _otpService.GenerateOtp();
                var expiresAt = DateTime.UtcNow.AddMinutes(10);

                // Create OTP record
                var otpVerification = new OtpVerification
                {
                    UserId = user.Id,
                    Email = request.Email,
                    OtpCode = otp,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                _context.OtpVerifications.Add(otpVerification);
                await _context.SaveChangesAsync();

                // ⚠️ DEBUGGING MODE: Send OTP email synchronously (not in background)
                // This allows you to:
                // 1. See errors immediately in the response
                // 2. Use breakpoints in EmailService
                // 3. Debug SMTP connection issues
                // 
                // In production, we'll move this back to Task.Run() so it doesn't block the API response
                // Send OTP email in background so API response isn't blocked by SMTP delays.
                // Errors will be logged by EmailService.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("📧 [Background] Starting OTP email send to {Email}", request.Email);
                        await _emailService.SendOtpEmailAsync(request.Email, otp, user.FirstName);
                        _logger.LogInformation("✅ [Background] OTP email sent successfully to {Email}", request.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "❌ [Background] Failed to send OTP email to {Email}: {Message}", request.Email, emailEx.Message);
                    }
                });

                _logger.LogInformation("✅ OTP process completed for {Email}", request.Email);

                return new OtpResponse
                {
                    Success = true,
                    Message = "OTP has been sent to your email. Valid for 10 minutes."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending OTP: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                return new OtpResponse
                {
                    Success = false,
                    Message = $"An error occurred while sending OTP: {ex.Message}"
                };
            }
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

                // Find valid OTP
                var otp = await _context.OtpVerifications
                    .Where(o => o.UserId == user.Id && 
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

                // Mark OTP as used
                otp.IsUsed = true;
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
