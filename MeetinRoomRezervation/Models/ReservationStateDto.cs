namespace MeetinRoomRezervation.Models
{
	public class ReservationStateDto
	{
		public List<SlotDto> SelectedSlots { get; set; } = new List<SlotDto>();
		public string Location { get; set; } 
		public string RoomName { get; set; } 
		public DateTime SelectedDate { get; set; } = DateTime.Today;
		public string RoomId { get; set; } 
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
