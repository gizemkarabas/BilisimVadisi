using MeetinRoomRezervation.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace MeetinRoomRezervation.Data
{
	public class MeetingRoom
	{
		public required string Id { get; set; }
		public string Name { get; set; }
		public string Location { get; set; }
		public int Capacity { get; set; }
		public double OccupancyRate { get; set; }
		public bool IsAvailable { get; set; }
		public List<ReservationDto> Reservations { get; set; } = new();

		[BsonIgnore]
		public List<string> AvailableSlots { get; set; } = new();
	}

}
