namespace MeetinRoomRezervation.Models
{
	public interface IReservationService
	{
		Task<List<MeetingRoom>> GetAllRoomsAsync();
		Task<bool> AddReservationAsync(Reservation reservation);
		Task<List<MeetingRoom>> GetAllRoomsWithOccupancyAsync();
		Task<List<string>> GetAvailableTimesAsync(string roomId);
	}
}
