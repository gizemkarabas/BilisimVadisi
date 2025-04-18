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

		public async Task<List<MeetingRoom>> GetAllRoomsAsync()
		{
			return await _context.Rooms.Find(_ => true).ToListAsync();
		}

		public async Task<bool> AddReservationAsync(Reservation reservation)
		{
			await _context.Reservations.InsertOneAsync(reservation);
			return true;
		}

		public async Task<List<MeetingRoom>> GetAllRoomsWithOccupancyAsync()
		{
			var rooms = await _context.Rooms.Find(_ => true).ToListAsync();
			foreach (var room in rooms)
			{
				var reservations = await _context.Reservations
					.Find(r => r.RoomId == room.Id)
					.ToListAsync();

				// Doluluk oranını hesapla (örnek: toplam rezervasyon süresi / toplam saat)
				room.OccupancyRate = CalculateOccupancyRate(reservations);
			}
			return rooms;
		}

		private int CalculateOccupancyRate(List<Reservation> reservations)
		{
			// Örnek hesaplama: toplam rezervasyon süresi / toplam saat
			var totalReservedHours = reservations.Sum(r => (r.EndTime - r.StartTime).TotalHours);
			var totalHours = 24 * 7; // Haftalık toplam saat
			return (int)((totalReservedHours / totalHours) * 100);
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
