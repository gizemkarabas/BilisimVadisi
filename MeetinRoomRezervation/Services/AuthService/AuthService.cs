using MeetinRoomRezervation.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;
using System.Security.Claims;
using static MeetinRoomRezervation.Components.Pages.Login;
using static MeetinRoomRezervation.Components.Pages.Register;

namespace MeetinRoomRezervation.Services
{

	public class AuthService : IAuthService
	{
		private readonly MongoDbContext _context;
		private readonly IMongoCollection<User> _userCollection;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ILogger<AuthService> _logger;
		public AuthService(MongoDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
		{
			_context = context;
			_userCollection = context.Users;
			_httpContextAccessor = httpContextAccessor;
			_logger = logger;
		}
		public async Task<User> LoginAsync(LoginInputModel model)
		{
			_logger.LogInformation("Login attempt for email: {Email}", model.Email);

			var user = await _context.Users.Find(u => u.Email == model.Email).FirstOrDefaultAsync();
			if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
			{
				var claims = new List<Claim>
					{
						new Claim(ClaimTypes.Name, user.Email),
						new Claim(ClaimTypes.NameIdentifier, user.Id),
						new Claim(ClaimTypes.Role, user.Role.ToString()),
						new Claim("Company", user.Company ?? ""),
						new Claim("CompanyOfficial", user.CompanyOfficial ?? ""),
						new Claim("FirstName", user.FirstName ?? ""),
						new Claim("LastName", user.LastName ?? "")
					};

				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var authProperties = new AuthenticationProperties
				{
					IsPersistent = true,
					ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
				};
				if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Response != null && !_httpContextAccessor.HttpContext.Response.HasStarted)
				{
					try
					{
						await _httpContextAccessor.HttpContext.SignInAsync(
							CookieAuthenticationDefaults.AuthenticationScheme,
							new ClaimsPrincipal(claimsIdentity),
							authProperties);

						Console.WriteLine($"Cookie başarıyla kaydedildi: {user.Email}");
					}
					catch (InvalidOperationException ex)
					{
						Console.WriteLine($"Cookie kaydetme hatası: {ex.Message}");
					}
				}
				else
				{
					Console.WriteLine("Response zaten başlamış, cookie kaydedilemedi");
				}

				return user;
			}


			return null;
		}
		public async Task<User> RegisterAsync(RegisterInputModel model)
		{
			_logger.LogInformation("Registration attempt for email: {Email}", model.Email);

			try
			{
				var existingUser = await _context.Users
				   .Find(u => u.Email == model.Email)
				   .FirstOrDefaultAsync();

				if (existingUser != null)
				{
					_logger.LogWarning("Registration failed - email already exists: {Email}", model.Email);
					throw new InvalidOperationException("Bu email adresi zaten kullanılıyor.");
				}

				var hash = BCrypt.Net.BCrypt.HashPassword(model.Password);

				var user = new User
				{
					Id = Guid.NewGuid().ToString(),
					Email = model.Email,
					PasswordHash = hash,
					CreatedAt = DateTime.UtcNow,
				};

				await _context.Users.InsertOneAsync(user);
				_logger.LogInformation("User registered successfully: {Email}", user.Email);

				return user;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
				throw;
			}
		}
		public async Task LogoutAsync()
		{
			var userEmail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
			_logger.LogInformation("Logout attempt for user: {Email}", userEmail);

			try
			{
				if (_httpContextAccessor.HttpContext != null &&
					_httpContextAccessor.HttpContext.Response != null &&
					!_httpContextAccessor.HttpContext.Response.HasStarted)
				{
					await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
					_logger.LogInformation("User logged out successfully: {Email}", userEmail);
				}
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning("Could not complete logout: {Error}", ex.Message);
			}
		}
		public async Task SignInUserAsync(User user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, user.Email),
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Role, user.Role.ToString()),
				new Claim("Company", user.Company ?? ""),
				new Claim("CompanyOfficial", user.CompanyOfficial ?? "")
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
			};

			try
			{
				if (_httpContextAccessor.HttpContext != null &&
					_httpContextAccessor.HttpContext.Response != null &&
					!_httpContextAccessor.HttpContext.Response.HasStarted)
				{
					await _httpContextAccessor.HttpContext.SignInAsync(
						CookieAuthenticationDefaults.AuthenticationScheme,
						new ClaimsPrincipal(claimsIdentity),
						authProperties);
				}
			}
			catch (InvalidOperationException)
			{
				// Response zaten başlamışsa, yok say
			}
		}
		public async Task<bool> IsEmailTaken(string email)
		{
			var existing = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
			return existing != null;
		}
		public async Task<bool> IsAdminAsync(string userId)
		{
			var user = await _context.Users
				.Find(u => u.Id == userId)
				.FirstOrDefaultAsync();

			return user?.Role == UserRole.Admin;
		}
		public async Task<UserRole> GetUserRoleAsync(string userId)
		{
			var user = await _context.Users
				.Find(u => u.Id == userId)
				.FirstOrDefaultAsync();

			return user?.Role ?? UserRole.User;
		}
		public async Task<User> GetCurrentUserAsync()
		{
			var httpContext = _httpContextAccessor.HttpContext;
			var userEmail = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
			if (!string.IsNullOrEmpty(userEmail))
			{
				var user = await _context.Users
					.Find(u => u.Email == userEmail)
					.FirstOrDefaultAsync();
				return user;
			}

			return null;
		}

	}

}
