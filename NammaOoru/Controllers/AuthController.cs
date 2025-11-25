using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NammaOoru.Models;
using NammaOoru.Services;

namespace NammaOoru.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">User registration details</param>
        /// <returns>Registration response with user information</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Send OTP to user's email for verification
        /// </summary>
        /// <param name="request">Email address to send OTP to</param>
        /// <returns>OTP sending response</returns>
        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<ActionResult<OtpResponse>> SendOtp([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.SendOtpAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Verify OTP sent to user's email
        /// </summary>
        /// <param name="request">Email and OTP code</param>
        /// <returns>Verification response with JWT token</returns>
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.VerifyOtpAsync(request);
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
    

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="request">Email and password credentials</param>
        /// <returns>Login response with JWT token</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request);
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Test endpoint to verify authenticated access
        /// </summary>
        /// <returns>Success message</returns>
        [HttpGet("test-auth")]
        [Authorize]
        public ActionResult<object> TestAuth()
        {
            var userId = User.FindFirst("id")?.Value;
            var email = User.FindFirst("email")?.Value;

            return Ok(new
            {
                message = "You are authenticated!",
                userId = userId,
                email = email
            });
        }
    }
}