using static MeetinRoomRezervation.Components.Pages.Login;
using static MeetinRoomRezervation.Components.Pages.Register;

namespace MeetinRoomRezervation.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterInputModel model);
        Task<bool> LoginAsync(LoginInputModel model);
        Task<bool> IsEmailTaken(string email);
    }

}
