using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public interface IReservationService
	{
		Task<string> AddReservationAsync(ReservationDto reservationDto);
		Task CancelReservationAsync(string reservationId);
		Task<List<ReservationDto>> GetReservationsByDateAsync(DateTime date);
		Task<List<ReservationDto>> GetUserReservationsAsync();
		Task<bool> DeleteReservationAsync(string reservationId);
		Task<List<ReservationDto>> GetAllReservationsAsync();
		Task<bool> UpdateReservationAsync(ReservationDto reservation);
		Task<bool> AdminDeleteReservationAsync(string reservationId);
		Task<User?> GetCurrentUserAsync();
	}
}
