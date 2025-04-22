namespace MeetinRoomRezervation.Models
{
	public interface IReservationService
	{
		Task<List<MeetingRoomDto>> GetAllRoomsAsync();
		Task<bool> AddReservationAsync(ReservationDto reservation);
		Task<List<string>> GetAvailableTimesAsync(string roomId);
		Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date);
		double CalculateOccupancyRate(List<ReservationDto> reservations, int capacity);
	}
}
