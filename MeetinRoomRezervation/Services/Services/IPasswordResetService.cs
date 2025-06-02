namespace MeetinRoomRezervation.Services
{
	public interface IPasswordResetService
	{
		Task<bool> SendPasswordResetEmailAsync(string email);
		Task<bool> ResetPasswordAsync(string token, string newPassword);
		Task<bool> ValidateResetTokenAsync(string token);
	}
}
