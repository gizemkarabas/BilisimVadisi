using MeetinRoomRezervation.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace MeetinRoomRezervation.Data
{
	public class Reservation
	{
		[BsonId]
		public string Id { get; set; } = "";

		[BsonElement("userId")]
		public string UserId { get; set; } = ""; // Kullanıcı ID'si

		[BsonElement("roomId")]
		public string RoomId { get; set; } = ""; // Oda ID'si

		[BsonElement("startTime")]
		public DateTime StartTime { get; set; }

		[BsonElement("endTime")]
		public DateTime EndTime { get; set; }

		[BsonElement("createdAt")]
		public DateTime CreatedAt { get; set; }

		[BsonElement("status")]
		public ReservationStatus Status { get; set; } = ReservationStatus.Active;

		[BsonElement("user")]
		public UserDto User { get; set; } = new();

		[BsonElement("room")]
		public MeetingRoomDto Room { get; set; } = new();

		[BsonElement("location")]
		public string Location { get; set; } = "";
	}

	public enum ReservationStatus
	{
		Active,
		Cancelled,
		Completed
	}
}

