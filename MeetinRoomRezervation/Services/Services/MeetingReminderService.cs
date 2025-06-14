using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Services.Services;
using MongoDB.Driver;
using System.Net;
using System.Net.Mail;

namespace MeetinRoomRezervation.Services
{

	public class MeetingReminderService : IMeetingReminderService
	{
		private readonly MongoDbContext _context;
		private readonly IConfiguration _configuration;
		private readonly ILogger<MeetingReminderService> _logger;
		private readonly IEmailTemplateService _emailTemplateService;

		public MeetingReminderService(
			MongoDbContext context,
			IConfiguration configuration,
			ILogger<MeetingReminderService> logger,
			IEmailTemplateService emailTemplateService)
		{
			_context = context;
			_configuration = configuration;
			_logger = logger;
			_emailTemplateService = emailTemplateService;
		}

		public async Task SendReminderEmailAsync(string email, string userName, string roomName, DateTime meetingTime, string meetingTitle)
		{
			try
			{
				var smtpHost = _configuration["Email:SmtpHost"];
				var smtpPortStr = _configuration["Email:SmtpPort"];
				var smtpUsername = _configuration["Email:Username"];
				var smtpPassword = _configuration["Email:Password"];
				var fromEmail = _configuration["Email:FromEmail"];
				var enableSslStr = _configuration["Email:EnableSsl"];

				if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr) ||
					string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) ||
					string.IsNullOrEmpty(fromEmail))
				{
					throw new InvalidOperationException("Email configuration is incomplete");
				}

				if (!int.TryParse(smtpPortStr, out int smtpPort))
				{
					throw new InvalidOperationException("Invalid SMTP port configuration");
				}

				bool enableSsl = bool.TryParse(enableSslStr, out bool ssl) ? ssl : true;

				var emailBody = _emailTemplateService.GetMeetingReminderTemplate(userName, roomName, meetingTime, meetingTitle);

				var mailMessage = new MailMessage
				{
					From = new MailAddress(fromEmail, "Toplantı Odası Rezervasyon"),
					Subject = $"🔔 Toplantı Hatırlatması - {roomName} ({meetingTime:HH:mm})",
					Body = emailBody,
					IsBodyHtml = true
				};

				mailMessage.To.Add(email);

				using var smtpClient = new SmtpClient(smtpHost, smtpPort)
				{
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential(smtpUsername, smtpPassword),
					EnableSsl = enableSsl,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					Timeout = 30000
				};

				_logger.LogInformation("Sending meeting reminder email to: {Email} for meeting at {MeetingTime}", email, meetingTime);
				await smtpClient.SendMailAsync(mailMessage);
				_logger.LogInformation("Meeting reminder email sent successfully to: {Email}", email);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending meeting reminder email to: {Email}", email);
				throw;
			}
		}

		public async Task CheckAndSendRemindersAsync()
		{
			try
			{
				var now = DateTime.UtcNow;
				var reminderTime = now.AddMinutes(5); // 5 dakika sonrası

				// 5 dakika sonra başlayacak toplantıları bul
				var upcomingMeetings = await _context.Reservations
					.Find(r => r.StartTime <= reminderTime &&
							  r.StartTime > now &&
							  r.Status == ReservationStatus.Confirmed &&
							  !r.ReminderSent) // Daha önce hatırlatma gönderilmemiş
					.ToListAsync();

				foreach (var meeting in upcomingMeetings)
				{
					try
					{
						// Kullanıcı bilgilerini al
						var user = await _context.Users.Find(u => u.Id == meeting.UserId).FirstOrDefaultAsync();
						if (user == null) continue;

						// Oda bilgilerini al
						var room = await _context.Rooms.Find(r => r.Id == meeting.RoomId).FirstOrDefaultAsync();
						if (room == null) continue;

						// Hatırlatma mailini gönder
						await SendReminderEmailAsync(
							user.Email,
							user.FirstName + " " + user.LastName,
							room.Name,
							meeting.StartTime,
							meeting.MeetingTitle

						);

						// Hatırlatma gönderildi olarak işaretle
						var update = Builders<Reservation>.Update.Set(r => r.ReminderSent, true);
						await _context.Reservations.UpdateOneAsync(r => r.Id == meeting.Id, update);

						_logger.LogInformation("Reminder sent for meeting {MeetingId} to user {UserEmail}", meeting.Id, user.Email);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error processing reminder for meeting {MeetingId}", meeting.Id);
					}
				}

				_logger.LogInformation("Processed {Count} meeting reminders", upcomingMeetings.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in CheckAndSendRemindersAsync");
			}
		}
	}
}
