using MeetinRoomRezervation.Services.Services;

namespace MeetinRoomRezervation.Services
{

	public class EmailTemplateService : IEmailTemplateService
	{
		public string GetMeetingReminderTemplate(string userName, string roomName, DateTime meetingTime, string meetingTitle)
		{
			return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Toplantı Hatırlatması</title>
            </head>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #515def;'>🔔 Toplantı Hatırlatması</h2>
                    <p>Merhaba <strong>{userName}</strong>,</p>
                    <p>Rezerve ettiğiniz toplantı <strong>5 dakika</strong> içinde başlayacak!</p>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #515def;'>📋 Toplantı Detayları</h3>
                        <p><strong>📅 Tarih:</strong> {meetingTime:dd MMMM yyyy}</p>
                        <p><strong>🕐 Saat:</strong> {meetingTime:HH:mm}</p>
                        <p><strong>🏢 Toplantı Odası:</strong> {roomName}</p>
                        <p><strong>📝 Başlık:</strong> {meetingTitle}</p>
                    </div>
                    
                    <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                        <p style='margin: 0; color: #856404;'>
                            <strong>⚠️ Hatırlatma:</strong> Lütfen toplantı odasına zamanında gelerek diğer katılımcıları bekletmeyiniz.
                        </p>
                    </div>
                    
                    <p>İyi toplantılar dileriz!</p>
                    
                    <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                    <p style='font-size: 12px; color: #666;'>
                        Bu e-posta Toplantı Odası Rezervasyon sistemi tarafından otomatik olarak gönderilmiştir.
                    </p>
                </div>
            </body>
            </html>";
		}
	}
}
