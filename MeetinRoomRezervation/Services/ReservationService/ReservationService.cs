using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Services.ReservationService;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MeetinRoomRezervation.Models
{
	public class ReservationService : IReservationService
	{
		private readonly MongoDbContext _context;

		public ReservationService(MongoDbContext context)
		{
			_context = context;
		}
		public async Task<MeetingRoomDto> GetRoomByIdAsync(string roomId)
		{
			try
			{
				//var objectId = new ObjectId(roomId);
				var filter = Builders<Data.MeetingRoom>.Filter.Eq("_id", roomId);
				var room = await _context.Rooms.Find(filter).FirstOrDefaultAsync();

				if (room == null)
				{
					return null;
				}

				return new MeetingRoomDto
				{
					Id = room.Id,
					Name = room.Name,
					Location = room.Location,
					Capacity = room.Capacity,
					IsAvailable = true,
					OccupancyRate = 0
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetRoomByIdAsync: {ex.Message}");
				throw;
			}
		}
		public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
		{
			var rooms = await _context.Rooms.Find(_ => true).ToListAsync();

			return rooms.Select(room => new MeetingRoomDto
			{
				Id = room.Id,
				Name = room.Name,
				Location = room.Location,
				Capacity = room.Capacity,
				IsAvailable = true,
				OccupancyRate = 0
			}).ToList();
		}
		public async Task<bool> AddReservationAsync(ReservationDto reservationDto)
		{
			try
			{
				// Önce bu saat için rezervasyon var mı kontrol et
				var existingReservationFilter = Builders<Reservation>.Filter.And(
					Builders<Reservation>.Filter.Eq(r => r.RoomId, reservationDto.RoomId),
					Builders<Reservation>.Filter.Eq(r => r.UserId, reservationDto.UserId),
					Builders<Reservation>.Filter.Lt(r => r.StartTime, reservationDto.EndTime),
					Builders<Reservation>.Filter.Gt(r => r.EndTime, reservationDto.StartTime)
				);

				var existingReservation = await _context.Reservations.Find(existingReservationFilter).FirstOrDefaultAsync();

				// Eğer bu saat için rezervasyon varsa, yeni rezervasyon yapma
				if (existingReservation != null)
				{
					return false;
				}

				// Yeni rezervasyon oluştur
				var reservation = new Reservation
				{
					Id = ObjectId.GenerateNewId().ToString(),
					RoomId = reservationDto.RoomId,
					//Room = reservationDto.Room.Name,
					StartTime = reservationDto.StartTime,
					EndTime = reservationDto.EndTime
				};


				await _context.Reservations.InsertOneAsync(reservation);

				// Rezervasyon yapıldıktan sonra odanın doluluk oranını güncelle
				await UpdateRoomOccupancyRate(reservationDto.RoomId, reservationDto.StartTime.Date);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in AddReservationAsync: {ex.Message}");
				return false;
			}
		}

		// Yeni metot: Odanın doluluk oranını güncelle
		private async Task UpdateRoomOccupancyRate(string roomId, DateTime date)
		{
			try
			{
				// Gün başlangıcı ve sonu için DateTime nesneleri oluştur
				var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
				var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

				// Belirli bir tarih için rezervasyonları al
				var reservationFilter = Builders<Reservation>.Filter.And(
					Builders<Reservation>.Filter.Eq(r => r.RoomId, roomId),
					Builders<Reservation>.Filter.Gte(r => r.StartTime, startOfDay),
					Builders<Reservation>.Filter.Lte(r => r.StartTime, endOfDay)
				);
				var roomReservations = await _context.Reservations.Find(reservationFilter).ToListAsync();

				// Gün içinde 09:00 - 18:00 arası saat slotları oluştur
				var startHour = 0;
				var endHour = 24;
				int reservedHoursCount = 0;

				for (int hour = startHour; hour < endHour; hour++)
				{
					var slotStart = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0);
					var slotEnd = slotStart.AddHours(1);

					// Eğer bu saat aralığında bir rezervasyon varsa slot dolu olur
					bool isReserved = roomReservations.Any(r =>
						(r.StartTime < slotEnd) && (r.EndTime > slotStart)
					);

					if (isReserved)
					{
						reservedHoursCount++;
					}
				}

				// Toplam saat sayısı (09:00 - 18:00 arası 9 saat)
				double totalHours = 24;

				// Rezerve edilen saat sayısı
				double reservedHours = reservedHoursCount;

				// Doluluk oranı hesaplama - %100'den fazla olmamalı
				double occupancyRate = Math.Min(100, Math.Round((reservedHours / totalHours) * 100, 2));

				// Odanın doluluk oranını güncelle
				var filter = Builders<Data.MeetingRoom>.Filter.Eq(r => r.Id, roomId);
				var update = Builders<Data.MeetingRoom>.Update.Set(r => r.OccupancyRate, occupancyRate);
				await _context.Rooms.UpdateOneAsync(filter, update);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in UpdateRoomOccupancyRate: {ex.Message}");
			}
		}
		public async Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date)
		{
			try
			{
				// Gün başlangıcı ve sonu için DateTime nesneleri oluştur
				var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
				var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

				var rooms = await _context.Rooms.Find(_ => true).ToListAsync();
				var result = new List<MeetingRoomDto>();

				foreach (var room in rooms)
				{
					// Belirli bir tarih için rezervasyonları al
					var reservationFilter = Builders<Reservation>.Filter.And(
						Builders<Reservation>.Filter.Eq(r => r.RoomId, room.Id),
						Builders<Reservation>.Filter.Gte(r => r.StartTime, startOfDay),
						Builders<Reservation>.Filter.Lte(r => r.StartTime, endOfDay)
					);
					var roomReservations = await _context.Reservations.Find(reservationFilter).ToListAsync();

					// Gün içinde 09:00 - 18:00 arası saat slotları oluştur
					var startHour = 9;
					var endHour = 18;
					int reservedHoursCount = 0;
					var now = DateTime.Now;
					bool isToday = date.Date == DateTime.Today;

					for (int hour = startHour; hour < endHour; hour++)
					{
						var slotStart = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0);
						var slotEnd = slotStart.AddHours(1);

						// Eğer bu saat aralığında bir rezervasyon varsa veya geçmiş bir saat ise slot dolu olur
						bool isReserved = roomReservations.Any(r =>
							(r.StartTime < slotEnd) && (r.EndTime > slotStart)
						);
						// Bugün için geçmiş saatleri de dolu olarak işaretle
						if (isToday && slotStart <= now)
						{
							reservedHoursCount++;
						}
						else if (isReserved)
						{
							reservedHoursCount++;
						}
					}

					// Toplam saat sayısı (09:00 - 18:00 arası 9 saat)
					double totalHours = 9;

					// Rezerve edilen saat sayısı
					double reservedHours = reservedHoursCount;

					// Doluluk oranı hesaplama - %100'den fazla olmamalı
					double occupancyRate = Math.Min(100, Math.Round((reservedHours / totalHours) * 100, 2));

					result.Add(new MeetingRoomDto
					{
						Id = room.Id!,
						Name = room.Name,
						Capacity = room.Capacity,
						Location = room.Location ?? "Bina 1 Koridor 2", // Konum bilgisi yoksa varsayılan değer
						OccupancyRate = occupancyRate,
						IsAvailable = occupancyRate < 100 // Eğer doluluk oranı %100'den azsa, oda müsait demektir
					});
				}

				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetAllRoomsWithOccupancyAsync: {ex.Message}");
				throw;
			}
		}

		public async Task<List<SlotDto>> GetSlotsWithStatusAsync(string roomId, DateTime date)
		{
			// Gün başlangıcı ve sonu için DateTime nesneleri oluştur
			var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
			var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

			// Tüm rezervasyonları çek
			var reservationFilter = Builders<Reservation>.Filter.And(
				Builders<Reservation>.Filter.Eq(r => r.RoomId, roomId),
				Builders<Reservation>.Filter.Gte(r => r.StartTime, startOfDay),
				Builders<Reservation>.Filter.Lte(r => r.StartTime, endOfDay)
			);
			var reservations = await _context.Reservations.Find(reservationFilter).ToListAsync();

			// Gün içinde 09:00 - 18:00 arası saat slotları oluştur
			var slots = new List<SlotDto>();
			var startHour = 9;
			var endHour = 18;
			var now = DateTime.Now;
			bool isToday = date.Date == DateTime.Today;

			for (int hour = startHour; hour < endHour; hour++)
			{
				var slotStart = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0);
				var slotEnd = slotStart.AddHours(1);


				// Eğer bu saat aralığında bir rezervasyon varsa slot dolu olur
				bool isReserved = reservations.Any(r =>
					(r.StartTime.ToLocalTime() < slotEnd) && (r.EndTime.ToLocalTime() > slotStart)
				);

				// Bugün için geçmiş saatleri de disable olarak işaretle
				bool isDisabled = (isToday && slotStart <= now) || isReserved;

				slots.Add(new SlotDto
				{
					StartTime = slotStart,
					EndTime = slotEnd,
					IsReserved = isReserved,
					IsDisabled = isDisabled
				});
			}

			return slots;
		}

		public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(string roomId, DateTime date)
		{
			var startHour = 9;
			var endHour = 18;
			var slotDuration = TimeSpan.FromHours(1); // 1 saatlik slotlar

			var reservations = await _context.Reservations
				.Find(r => r.RoomId == roomId && r.StartTime.Date == date.Date)
				.ToListAsync();

			var reservedHours = reservations
				.Select(r => r.StartTime.TimeOfDay)
				.ToHashSet();

			var slots = new List<TimeSpan>();

			for (int hour = startHour; hour < endHour; hour++)
			{
				var slot = TimeSpan.FromHours(hour);
				if (!reservedHours.Contains(slot))
				{
					slots.Add(slot);
				}
			}

			return slots;
		}

		public async Task<List<ReservationDto>> GetUserReservations(string userId)
		{
			var filter = Builders<Reservation>.Filter.Eq(r => r.UserId, userId);
			var reservations = await _context.Reservations.Find(filter).ToListAsync();
			return reservations.Select(reservation => new ReservationDto
			{
				Id = reservation.Id,
				RoomId = reservation.RoomId,
				UserId = reservation.UserId,
				StartTime = reservation.StartTime,
				EndTime = reservation.EndTime,
			}).ToList();

		}
		public async Task CancelReservationAsync(string reservationId)
		{
			var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservationId);
			await _context.Reservations.DeleteOneAsync(filter);
		}
		public async Task<List<ReservationDto>> GetAllReservationsAsync()
		{
			var reservations = await _context.Reservations.Find(_ => true).ToListAsync();
			var result = new List<ReservationDto>();
			foreach (var reservation in reservations)
			{
				string userEmail = null;
				if (!string.IsNullOrEmpty(reservation.UserId))
				{
					var userFilter = Builders<User>.Filter.Eq("_id", reservation.UserId);
					var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();
					userEmail = user?.Email;
				}

				string roomName = null;
				if (!string.IsNullOrEmpty(reservation.RoomId))
				{
					var roomFilter = Builders<Data.MeetingRoom>.Filter.Eq("_id", reservation.RoomId);
					var room = await _context.Rooms.Find(roomFilter).FirstOrDefaultAsync();
					roomName = room?.Name;
				}

				result.Add(new ReservationDto
				{
					Id = reservation.Id,
					UserId = reservation.UserId,
					RoomId = reservation.RoomId,
					UserEmail = userEmail,
					RoomName = roomName,
					StartTime = reservation.StartTime,
					EndTime = reservation.EndTime
				});
			}

			return result;

		}
		public async Task<bool> UpdateReservationAsync(ReservationDto updated)
		{
			var reservation = new Reservation
			{
				Id = updated.Id,
				UserId = updated.UserId,
				RoomId = updated.RoomId,
				StartTime = updated.StartTime,
				EndTime = updated.EndTime,

			};
			var filter = Builders<Reservation>.Filter.Eq(r => r.Id, updated.Id);
			var update = Builders<Reservation>.Update
				.Set(r => r.StartTime, updated.StartTime)
				.Set(r => r.EndTime, updated.EndTime);

			var result = await _context.Reservations.UpdateOneAsync(filter, update);
			return result.ModifiedCount > 0;
		}
		public async Task<bool> DeleteReservationAsync(string reservationId)
		{
			var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservationId);
			var result = await _context.Reservations.DeleteOneAsync(filter);
			return result.DeletedCount > 0;
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

		public async Task<bool> AddRoomAsync(MeetingRoomDto roomDto)
		{
			try
			{
				var room = new Data.MeetingRoom
				{
					Id = ObjectId.GenerateNewId().ToString(),
					Name = roomDto.Name,
					Location = roomDto.Location,
					Capacity = roomDto.Capacity,
					OccupancyRate = 0,
					IsAvailable = true
				};

				await _context.Rooms.InsertOneAsync(room);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in AddRoomAsync: {ex.Message}");
				return false;
			}
		}
		public async Task<List<ReservationDto>> GetReservationsByDateAsync(DateTime date)
		{
			var start = date.Date;
			var end = start.AddDays(1);

			var filter = Builders<Reservation>.Filter.And(
				Builders<Reservation>.Filter.Gte(r => r.StartTime, start),
				Builders<Reservation>.Filter.Lt(r => r.StartTime, end)
			);

			var reservations = await _context.Reservations.Find(filter).ToListAsync();

			var result = new List<ReservationDto>();
			foreach (var reservation in reservations)
			{
				string userEmail = null;
				if (!string.IsNullOrEmpty(reservation.UserId))
				{
					var userFilter = Builders<User>.Filter.Eq("_id", reservation.UserId);
					var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();
					userEmail = user?.Email;
				}

				string roomName = null;
				if (!string.IsNullOrEmpty(reservation.RoomId))
				{
					var roomFilter = Builders<Data.MeetingRoom>.Filter.Eq("_id", reservation.RoomId);
					var room = await _context.Rooms.Find(roomFilter).FirstOrDefaultAsync();
					roomName = room?.Name;
				}

				result.Add(new ReservationDto
				{
					Id = reservation.Id,
					UserId = reservation.UserId,
					RoomId = reservation.RoomId,
					UserEmail = userEmail,
					RoomName = roomName,
					StartTime = reservation.StartTime,
					EndTime = reservation.EndTime
				});
			}

			return result;
		}

	}

}
