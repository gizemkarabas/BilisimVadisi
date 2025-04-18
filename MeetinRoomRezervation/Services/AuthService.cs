using MeetinRoomRezervation.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using static MeetinRoomRezervation.Components.Pages.Login;
using static MeetinRoomRezervation.Components.Pages.Register;

namespace MeetinRoomRezervation.Services
{

	public class AuthService : IAuthService
	{
		private readonly MongoDbContext _context;

		public AuthService(MongoDbContext context)
		{
			_context = context;
		}

		public async Task<bool> RegisterAsync(RegisterInputModel model)
		{
			if (await IsEmailTaken(model.Surname))
				return false;

			var hash = HashPassword(model.Password);

			var user = new User
			{
				Email = model.Email,
				PasswordHash = hash
			};

			await _context.Users.InsertOneAsync(user);
			return true;
		}
		public async Task<bool> LoginAsync(LoginInputModel model)
		{
			var user = await _context.Users.Find(u => u.Email == model.Email).FirstOrDefaultAsync();
			if (user is null)
				return false;

			var inputHash = HashPassword(model.Password);
			if (user.PasswordHash != inputHash)
				return false;

			// Eğer giriş başarılıysa, cookie veya ClaimsPrincipal oluşturulabilir
			// Şimdilik sadece bool döndürüyoruz, AuthenticationStateProvider ile tam oturum sistemi kuracağız

			return true;
		}

		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(password);
			var hash = sha256.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}

		public async Task<bool> IsEmailTaken(string email)
		{
			var existing = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
			return existing != null;
		}

	
	}

}
