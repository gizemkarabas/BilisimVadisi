﻿using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public class RoomService : IRoomService
	{
		private readonly MongoDbContext _context;

		public RoomService(MongoDbContext context)
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
		public async Task UpdateRoomAsync(MeetingRoomDto roomDto)
		{
			var filter = Builders<MeetingRoom>.Filter.Eq(r => r.Id, roomDto.Id);

			var update = Builders<MeetingRoom>.Update
				.Set(r => r.Name, roomDto.Name)
				.Set(r => r.Capacity, roomDto.Capacity);

			if (!string.IsNullOrEmpty(roomDto.Location))
			{
				update = update.Set(r => r.Location, roomDto.Location);
			}

			await _context.Rooms.UpdateOneAsync(filter, update);
		}
		public async Task DeleteRoomAsync(string roomId)
		{
			var filter = Builders<MeetingRoom>.Filter.Eq(r => r.Id, roomId);
			await _context.Rooms.DeleteOneAsync(filter);
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

	}

}
