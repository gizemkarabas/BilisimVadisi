using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
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
		//public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
		//{
		//	var rooms = await _context.Rooms.Find(_ => true).ToListAsync();
		//	return rooms.Select(room => new MeetingRoomDto
		//	{
		//		Id = room.Id,
		//		Name = room.Name,
		//		Capacity = room.Capacity,
		//		Location = room.Location,
		//		IsAvailable = room.IsAvailable,
		//		OccupancyRate = room.OccupancyRate
		//	}).ToList();
		//}
		//public async Task<MeetingRoomDto> GetRoomByIdAsync(string roomId)
		//{
		//	try
		//	{
		//		//var objectId = new ObjectId(roomId);
		//		var filter = Builders<Data.MeetingRoom>.Filter.Eq("_id", roomId);
		//		var room = await _context.Rooms.Find(filter).FirstOrDefaultAsync();

		//		if (room == null)
		//		{
		//			return null;
		//		}

		//		return new MeetingRoomDto
		//		{
		//			Id = room.Id,
		//			Name = room.Name,
		//			Location = room.Location,
		//			Capacity = room.Capacity,
		//			IsAvailable = true,
		//			OccupancyRate = 0
		//		};
		//	}
		//	catch (Exception ex)
		//	{
		//		Console.WriteLine($"Error in GetRoomByIdAsync: {ex.Message}");
		//		throw;
		//	}
		//}
		//public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
		//{
		//	var rooms = await _context.Rooms.Find(_ => true).ToListAsync();

		//	return rooms.Select(room => new MeetingRoomDto
		//	{
		//		Id = room.Id,
		//		Name = room.Name,
		//		Location = room.Location,
		//		Capacity = room.Capacity,
		//		IsAvailable = true,
		//		OccupancyRate = 0
		//	}).ToList();
		//}
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

	}

}
