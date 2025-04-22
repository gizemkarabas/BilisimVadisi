using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ReservationDto
{
	public string? Id { get; set; }

	public string UserEmail { get; set; } = string.Empty;
	public string RoomId { get; set; } = string.Empty;
	public string RoomName { get; set; } = string.Empty;

	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
}
