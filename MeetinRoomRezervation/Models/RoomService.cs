//using MongoDB.Driver;

//namespace MeetinRoomRezervation.Models
//{
//	public class RoomService : IRoomService
//	{
//			private readonly MongoDbContext _context;

//			public RoomService(MongoDbContext context)
//			{
//				_context = context;
//			}

	

//		public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
//		{
//			var rooms= await _context.Rooms.Find(_ => true).ToListAsync();
//			return rooms.Select(room => new MeetingRoomDto
//			{
//				Id = room.Id,
//				Name = room.Name,
//				Capacity = room.Capacity,
//				Location = room.Location,
//				IsAvailable = room.IsAvailable,
//				OccupancyRate = room.OccupancyRate
//			}).ToList();
//		}

//		public async Task<MeetingRoomDto> GetRoomByIdAsync(string roomId)
//		{
//			try
//			{
//				//var objectId = new ObjectId(roomId);
//				var filter = Builders<Data.MeetingRoom>.Filter.Eq("_id", roomId);
//				var room = await _context.Rooms.Find(filter).FirstOrDefaultAsync();

//				if (room == null)
//				{
//					return null;
//				}

//				return new MeetingRoomDto
//				{
//					Id = room.Id,
//					Name = room.Name,
//					Location = room.Location,
//					Capacity = room.Capacity,
//					IsAvailable = true,
//					OccupancyRate = 0
//				};
//			}
//			catch (Exception ex)
//			{
//				Console.WriteLine($"Error in GetRoomByIdAsync: {ex.Message}");
//				throw;
//			}
//		}
//		public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
//		{
//			var rooms = await _context.Rooms.Find(_ => true).ToListAsync();

//			return rooms.Select(room => new MeetingRoomDto
//			{
//				Id = room.Id,
//				Name = room.Name,
//				Location = room.Location,
//				Capacity = room.Capacity,
//				IsAvailable = true,
//				OccupancyRate = 0
//			}).ToList();
//		}
//	}

//}
