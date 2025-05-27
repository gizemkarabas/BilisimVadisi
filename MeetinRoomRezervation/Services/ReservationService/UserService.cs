using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public class UserService : IUserService
	{
		private readonly MongoDbContext _context;

		public UserService(MongoDbContext context)
		{
			_context = context;
		}
		public async Task<List<UserDto>> GetAllUsersAsync()
		{
			try
			{
				var users = await _context.Users.Find(_ => true).ToListAsync();
				return users.Select(user => new UserDto
				{
					Id = user.Id,
					FirstName = user.FirstName,
					LastName = user.LastName,
					Email = user.Email,
					Company = user.Company,
					CompanyOfficial = user.CompanyOfficial,
					ContactPhone = user.ContactPhone,
					MonthlyUsageLimit = user.MonthlyUsageLimit,
					UsedThisMonth = user.UsedThisMonth,
					IsActive = user.IsActive

				}).ToList();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetAllUsersAsync: {ex.Message}");
				return new List<UserDto>();
			}
		}

		public async Task<bool> UpdateUserStatusAsync(string userId, bool isActive)
		{
			try
			{
				var filter = Builders<User>.Filter.Eq("_id", userId);
				var update = Builders<User>.Update.Set(u => u.IsActive, isActive);

				var result = await _context.Users.UpdateOneAsync(filter, update);
				return result.ModifiedCount > 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in UpdateUserStatusAsync: {ex.Message}");
				return false;
			}
		}
		public async Task<bool> DeleteUserAsync(string userId)
		{
			try
			{
				var filter = Builders<User>.Filter.Eq("_id", userId);
				var result = await _context.Users.DeleteOneAsync(filter);
				return result.DeletedCount > 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in DeleteUserAsync: {ex.Message}");
				return false;
			}
		}

		public async Task<UserDto> GetUserByIdAsync(string userId)
		{
			try
			{
				var filter = Builders<User>.Filter.Eq("_id", userId);
				var user = await _context.Users.Find(filter).FirstOrDefaultAsync();

				if (user == null)
				{
					return null;
				}

				return new UserDto
				{
					Id = user.Id,
					FirstName = user.FirstName,
					LastName = user.LastName,
					Email = user.Email,
					IsActive = user.IsActive,
					// Diğer kullanıcı özellikleri buraya eklenebilir
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetUserByIdAsync: {ex.Message}");
				return null;
			}
		}
		public async Task AddUserAsync(UserDto userDto)
		{
			var user = new User
			{
				Id = ObjectId.GenerateNewId().ToString(),
				Email = userDto.Email,
				FirstName = userDto.FirstName,
				LastName = userDto.LastName,
				Company = userDto.Company,
				CompanyOfficial = userDto.CompanyOfficial,
				ContactPhone = userDto.ContactPhone,
				MonthlyUsageLimit = userDto.MonthlyUsageLimit,
				UsedThisMonth = userDto.UsedThisMonth,
				IsActive = userDto.IsActive
			};
			await _context.Users.InsertOneAsync(user);
		}

		public async Task UpdateMonthlyUsageAsync()
		{
			var users = await _context.Users.Find(_ => true).ToListAsync();
			var now = DateTime.UtcNow;
			var monthStart = new DateTime(now.Year, now.Month, 1);
			var nextMonth = monthStart.AddMonths(1);

			foreach (var user in users)
			{
				var userReservations = await _context.Reservations
					.Find(r => r.UserId == user.Id &&
							   r.StartTime >= monthStart &&
							   r.StartTime < nextMonth)
					.ToListAsync();

				double totalHours = userReservations
					.Sum(r => (r.EndTime - r.StartTime).TotalHours);

				var update = Builders<User>.Update.Set(u => u.UsedThisMonth, (int)Math.Round(totalHours));

				var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);

				await _context.Users.UpdateOneAsync(filter, update);
			}
		}

		public async Task<bool> UpdateUserAsync(UserDto userDto)
		{
			try
			{
				var filter = Builders<User>.Filter.Eq("_id", userDto.Id);
				var update = Builders<User>.Update
					.Set(u => u.FirstName, userDto.FirstName)
					.Set(u => u.LastName, userDto.LastName)
					.Set(u => u.Email, userDto.Email)
					.Set(u => u.Company, userDto.Company)
					.Set(u => u.CompanyOfficial, userDto.CompanyOfficial)
					.Set(u => u.ContactPhone, userDto.ContactPhone)
					.Set(u => u.MonthlyUsageLimit, userDto.MonthlyUsageLimit)
					.Set(u => u.IsActive, userDto.IsActive);

				var result = await _context.Users.UpdateOneAsync(filter, update);
				return result.ModifiedCount > 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in UpdateUserAsync: {ex.Message}");
				return false;
			}
		}
	}
}
