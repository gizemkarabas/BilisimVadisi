using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Data
{
	public class Reservation
	{
		public string Id { get; set; }
		public string UserId { get; set; }
		public UserDto User { get; set; }
		public string RoomId { get; set; }
		public MeetingRoom Room { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
	}
}