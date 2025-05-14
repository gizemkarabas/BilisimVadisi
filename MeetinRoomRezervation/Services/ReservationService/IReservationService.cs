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
		Task<List<ReservationDto>> GetAllReservationsAsync();
		Task<bool> UpdateReservationAsync(ReservationDto reservation);
		Task<bool> DeleteReservationAsync(string reservationId);
		Task<List<UserDto>> GetAllUsersAsync();
		Task<bool> UpdateUserStatusAsync(string userId, bool isActive);
		Task<bool> DeleteUserAsync(string userId);
		Task AddUserAsync(UserDto userDto);
		Task UpdateMonthlyUsageAsync();

	}
}
