using MeetinRoomRezervation.Data;
using static MeetinRoomRezervation.Components.Pages.Login;
using static MeetinRoomRezervation.Components.Pages.Register;

namespace MeetinRoomRezervation.Services
{
	public interface IAuthService
	{
		Task<User> RegisterAsync(RegisterInputModel model);
		Task<User> LoginAsync(LoginInputModel model);
		Task LogoutAsync();
		Task<bool> IsEmailTaken(string email);
		Task<UserRole> GetUserRoleAsync(string userId);
		Task<User> GetCurrentUserAsync();
	}

}
