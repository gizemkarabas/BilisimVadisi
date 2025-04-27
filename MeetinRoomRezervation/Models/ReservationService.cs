using MeetinRoomRezervation.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

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
				var filter = Builders<MeetingRoom>.Filter.Eq("_id", roomId);
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
			var reservation = new Reservation
			{
				Id= ObjectId.GenerateNewId().ToString(),
				UserEmail = reservationDto.UserEmail,
				RoomId = reservationDto.RoomId,
				RoomName = reservationDto.RoomName,
				StartTime = reservationDto.StartTime,
				EndTime = reservationDto.EndTime
			};
			await _context.Reservations.InsertOneAsync(reservation);
			return true;
		}

		public double CalculateOccupancyRate(List<Reservation> reservations, int capacity)
		{
			if (capacity <= 0) return 100;
			return Math.Min(100, (reservations.Count * 1.0 / capacity) * 100);
		}

		public async Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date)
		{
			var rooms = await _context.Rooms.Find(_ => true).ToListAsync();
			var reservations = await _context.Reservations
				.Find(r => r.StartTime.Date == date.Date)
				.ToListAsync();

			var result = rooms.Select(room =>
			{
				var roomReservations = reservations.Where(r => r.RoomId == room.Id).ToList();
				var occupancy = CalculateOccupancyRate(roomReservations, room.Capacity);
				return new MeetingRoomDto
				{
					Id = room.Id!,
					Name = room.Name,
					Capacity = room.Capacity,
					Location = "Bina 1 Koridor 2", 
					OccupancyRate = occupancy,
					IsAvailable = occupancy < 100
				};
			}).ToList();

			return result;
		}

		public async Task<List<string>> GetAvailableTimesAsync(string roomId)
		{
			var reservations = await _context.Reservations
				.Find(r => r.RoomId == roomId)
				.ToListAsync();

			var availableTimes = new List<string>();
			var startOfDay = DateTime.Today;
			var endOfDay = startOfDay.AddDays(1);

			for (var time = startOfDay; time < endOfDay; time = time.AddHours(1))
			{
				if (!reservations.Any(r => r.StartTime <= time && r.EndTime > time))
				{
					availableTimes.Add(time.ToString("HH:mm"));
				}
			}

			return availableTimes;
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


	}

}
