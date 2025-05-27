using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public interface IUserService
	{
		Task<List<UserDto>> GetAllUsersAsync();
		Task<bool> UpdateUserStatusAsync(string userId, bool isActive);
		Task<bool> DeleteUserAsync(string userId);
		Task AddUserAsync(UserDto userDto);
		Task UpdateMonthlyUsageAsync();
		Task<bool> UpdateUserAsync(UserDto userDto);

	}
}
