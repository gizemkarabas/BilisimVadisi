using MeetinRoomRezervation.Components.Pages;
using MeetinRoomRezervation.Data;

namespace MeetinRoomRezervation.Models
{
	public interface IReservationService
	{
		Task<MeetingRoomDto> GetRoomByIdAsync(string roomId);
		Task<List<MeetingRoomDto>> GetAllRoomsAsync();
		Task<bool> AddReservationAsync(ReservationDto reservationDto);
		Task<List<string>> GetAvailableTimesAsync(string roomId);
		Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date);
		Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(string roomId, DateTime date);
	}
}
