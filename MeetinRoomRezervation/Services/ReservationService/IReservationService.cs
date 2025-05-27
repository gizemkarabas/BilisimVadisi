using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public interface IReservationService
	{
		Task<bool> AddReservationAsync(ReservationDto reservationDto);
		Task<List<ReservationDto>> GetUserReservations(string userId);
		Task CancelReservationAsync(string reservationId);
		Task<List<ReservationDto>> GetAllReservationsAsync();
		Task<bool> UpdateReservationAsync(ReservationDto reservation);
		Task<bool> DeleteReservationAsync(string reservationId);
		Task<List<ReservationDto>> GetReservationsByDateAsync(DateTime date);



	}
}
