namespace MeetinRoomRezervation.Models
{
	public class ReservationDto
	{
		public List<SlotDto> SelectedSlots { get; set; } = new List<SlotDto>();
		public string Id { get; set; }
		public string UserId { get; set; }
		public string UserEmail { get; set; }
		public UserDto User { get; set; }
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public MeetingRoomDto Room { get; set; }
		public string Location { get; set; }
		public DateTime SelectedDate { get; set; } = DateTime.Today;
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
	}
	public class SlotDto
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public bool IsReserved { get; set; } = false;
		public bool IsDisabled { get; set; } = false;
		public bool IsSelected { get; set; } = false;
	}
}
