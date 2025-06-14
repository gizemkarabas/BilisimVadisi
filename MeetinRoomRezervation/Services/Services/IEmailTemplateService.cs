namespace MeetinRoomRezervation.Services.Services
{
	public interface IEmailTemplateService
	{
		string GetMeetingReminderTemplate(string userName, string roomName, DateTime meetingTime, string meetingTitle);
	}
}
