using MeetinRoomRezervation.Data;
using MongoDB.Driver;

namespace MeetinRoomRezervation.Services
{
	public class SeedDataService
	{
		private readonly MongoDbContext _context;

		public SeedDataService(MongoDbContext context)
		{
			_context = context;
		}

		public async Task SeedAdminUserAsync()
		{
			var adminExists = await _context.Users
				.Find(u => u.Role == UserRole.Admin)
				.AnyAsync();

			if (!adminExists)
			{
				var admin = new User
				{
					Id = Guid.NewGuid().ToString(),
					Email = "admin@example.com",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
					Company = "Sistem Yönetimi",
					FirstName = "Gizem",
					CompanyOfficial = "Admin",
					ContactPhone = "0000000000",
					Role = UserRole.Admin,
					CreatedAt = DateTime.UtcNow
				};

				await _context.Users.InsertOneAsync(admin);
			}
		}
	}
}
