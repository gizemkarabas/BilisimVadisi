using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MeetinRoomRezervation.Models
{
	public class ReservationDto
	{
		public string Id { get; set; }
		public string UserEmail { get; set; }
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public string Location { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
	}
}
