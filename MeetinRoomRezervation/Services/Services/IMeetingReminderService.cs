namespace MeetinRoomRezervation.Services.Services
{
	public interface IMeetingReminderService
	{
		Task SendReminderEmailAsync(string email, string userName, string roomName, DateTime meetingTime, string meetingTitle);
		Task CheckAndSendRemindersAsync();
	}
}
