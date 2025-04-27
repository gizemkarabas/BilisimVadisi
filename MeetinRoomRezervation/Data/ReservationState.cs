using static MeetinRoomRezervation.Components.Pages.ReservationForm;

namespace MeetinRoomRezervation.Data
{
	public class ReservationState
	{
		public List<Slot> SelectedSlots { get; set; }
		public string Location { get; set; } 
		public string RoomName { get; set; } 
		public DateTime SelectedDate { get; set; }
		public string RoomId { get; set; } 
	}

	public class Slot
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public bool IsReserved { get; set; } = false;
	}

}
