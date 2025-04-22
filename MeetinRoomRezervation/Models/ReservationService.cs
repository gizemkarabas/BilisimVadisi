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

		public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
		{
			return await _context.Rooms.Find(_ => true).ToListAsync();
		}

		public async Task<bool> AddReservationAsync(ReservationDto reservation)
		{
			await _context.Reservations.InsertOneAsync(reservation);
			return true;
		}

		public double CalculateOccupancyRate(List<ReservationDto> reservations, int capacity)
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
					Location = "Bina 1 Koridor 2", // test için sabit
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
	}

}
