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
		private readonly ILogger<ReservationService> _logger;

		public ReservationService(MongoDbContext context, ILogger<ReservationService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<bool> AddReservationAsync(ReservationDto reservationDto)
		{
			_logger.LogInformation("Creating reservation for room {RoomId} by user {UserId} from {StartTime} to {EndTime}",
				reservationDto.RoomId, reservationDto.UserId, reservationDto.StartTime, reservationDto.EndTime);

			try
			{
				var existingReservationFilter = Builders<Reservation>.Filter.And(
					Builders<Reservation>.Filter.Eq(r => r.RoomId, reservationDto.RoomId),
					Builders<Reservation>.Filter.Eq(r => r.UserId, reservationDto.UserId),
					Builders<Reservation>.Filter.Lt(r => r.StartTime, reservationDto.EndTime),
					Builders<Reservation>.Filter.Gt(r => r.EndTime, reservationDto.StartTime)
			);

				var existingReservation = await _context.Reservations.Find(existingReservationFilter).FirstOrDefaultAsync();

				if (existingReservation != null)
				{
					return false;
				}

				var reservation = new Reservation
				{
					Id = ObjectId.GenerateNewId().ToString(),
					RoomId = reservationDto.RoomId,
					//Room = reservationDto.Room.Name,
					StartTime = reservationDto.StartTime,
					EndTime = reservationDto.EndTime
				};

				await _context.Reservations.InsertOneAsync(reservation);

				await UpdateRoomOccupancyRate(reservationDto.RoomId, reservationDto.StartTime.Date);

				_logger.LogInformation("Reservation created successfully with ID: {ReservationId}", reservation.Id);
				return true;

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in AddReservationAsync: {ex.Message}");
				_logger.LogError(ex, "Failed to create reservation for room {RoomId}", reservationDto.RoomId);
				return false;
			}
		}
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
			_logger.LogInformation("Deleting reservation: {ReservationId}", reservationId);

			try
			{
				var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservationId);
				var result = await _context.Reservations.DeleteOneAsync(filter);
				return result.DeletedCount > 0;
				_logger.LogInformation("Reservation deleted successfully: {ReservationId}", reservationId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete reservation: {ReservationId}", reservationId);
				throw;
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
