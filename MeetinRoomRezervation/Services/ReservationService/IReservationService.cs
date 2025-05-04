using MeetinRoomRezervation.Components.Pages;
using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public interface IReservationService
	{
		Task<MeetingRoomDto> GetRoomByIdAsync(string roomId);
		Task<List<MeetingRoomDto>> GetAllRoomsAsync();
		Task<bool> AddReservationAsync(ReservationDto reservationDto);
		Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(string roomId, DateTime date);
		Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date);
		Task<List<SlotDto>> GetSlotsWithStatusAsync(string roomId, DateTime date);
		Task<List<ReservationDto>> GetUserReservations(string email);
		Task CancelReservationAsync(string reservationId);

	}
}
