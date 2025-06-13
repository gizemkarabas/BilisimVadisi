using MeetinRoomRezervation.Data;
using MongoDB.Driver;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace MeetinRoomRezervation.Services
{
	public class PasswordResetService : IPasswordResetService
	{
		private readonly MongoDbContext _context;
		private readonly IConfiguration _configuration;
		private readonly ILogger<PasswordResetService> _logger;

		public PasswordResetService(MongoDbContext context, IConfiguration configuration, ILogger<PasswordResetService> logger)
		{
			_context = context;
			_configuration = configuration;
			_logger = logger;
		}

		public async Task<bool> SendPasswordResetEmailAsync(string email)
		{
			try
			{
				var user = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
				if (user == null)
				{
					return false;
				}

				var resetToken = GenerateResetToken();
				var resetTokenExpiry = DateTime.UtcNow.AddHours(1);

				// Kullanıcıya reset token'ı kaydet
				var update = Builders<User>.Update
					.Set(u => u.ResetToken, resetToken)
					.Set(u => u.ResetTokenExpiry, resetTokenExpiry);

				await _context.Users.UpdateOneAsync(u => u.Id == user.Id, update);

				// E-posta gönder
				await SendResetEmailAsync(email, resetToken);

				_logger.LogInformation("Password reset email sent to: {Email}", email);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending password reset email to: {Email}", email);
				return false;
			}
		}

		public async Task<bool> ResetPasswordAsync(string token, string newPassword)
		{
			try
			{
				var user = await _context.Users.Find(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow).FirstOrDefaultAsync();

				if (user == null)
				{
					return false;
				}

				// Yeni şifreyi hash'le
				var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

				// Şifreyi güncelle ve token'ı temizle
				var update = Builders<User>.Update
					.Set(u => u.PasswordHash, hashedPassword)
					.Unset(u => u.ResetToken)
					.Unset(u => u.ResetTokenExpiry);

				await _context.Users.UpdateOneAsync(u => u.Id == user.Id, update);

				_logger.LogInformation("Password reset successfully for user: {Email}", user.Email);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error resetting password for token: {Token}", token);
				return false;
			}
		}

		public async Task<bool> ValidateResetTokenAsync(string token)
		{
			var user = await _context.Users.Find(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow).FirstOrDefaultAsync();
			return user != null;
		}

		private string GenerateResetToken()
		{
			using var rng = RandomNumberGenerator.Create();
			var bytes = new byte[32];
			rng.GetBytes(bytes);
			return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
		}

		private async Task SendResetEmailAsync(string email, string resetToken)
		{
			try
			{
				var smtpHost = _configuration["Email:SmtpHost"];
				var smtpPortStr = _configuration["Email:SmtpPort"];
				var smtpUsername = _configuration["Email:Username"];
				var smtpPassword = _configuration["Email:Password"];
				var fromEmail = _configuration["Email:FromEmail"];
				var baseUrl = _configuration["BaseUrl"];
				var enableSslStr = _configuration["Email:EnableSsl"];

				// Null kontrolleri
				if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr) ||
					string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) ||
					string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(baseUrl))
				{
					throw new InvalidOperationException("Email configuration is incomplete");
				}

				if (!int.TryParse(smtpPortStr, out int smtpPort))
				{
					throw new InvalidOperationException("Invalid SMTP port configuration");
				}

				bool enableSsl = bool.TryParse(enableSslStr, out bool ssl) ? ssl : true;

				var resetUrl = $"{baseUrl}/reset-password?token={resetToken}";

				var mailMessage = new MailMessage
				{
					From = new MailAddress(fromEmail, "Toplantı Odası Rezervasyon"),
					Subject = "Şifre Sıfırlama",
					Body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Şifre Sıfırlama</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #0095ff;'>Şifre Sıfırlama</h2>
                        <p>Merhaba,</p>
                        <p>Toplantı Odası Rezervasyon sistemi için şifre sıfırlama talebinde bulundunuz.</p>
                        <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayınız:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' 
                               style='background-color: #0095ff; color: white; padding: 12px 24px; 
                                      text-decoration: none; border-radius: 4px; display: inline-block;'>
                                Şifremi Sıfırla
                            </a>
                        </div>
                        <p>Eğer buton çalışmıyorsa, aşağıdaki linki kopyalayıp tarayıcınıza yapıştırabilirsiniz:</p>
                        <p style='word-break: break-all; background-color: #f5f5f5; padding: 10px; border-radius: 4px;'>
                            {resetUrl}
                        </p>
                        <p><strong>Önemli:</strong> Bu link 1 saat geçerlidir.</p>
                        <p>Eğer bu isteği siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                        <p style='font-size: 12px; color: #666;'>
                            Bu e-posta Toplantı Odası Rezervasyon sistemi tarafından otomatik olarak gönderilmiştir.
                        </p>
                    </div>
                </body>
                </html>
            ",
					IsBodyHtml = true
				};

				mailMessage.To.Add(email);

				using var smtpClient = new SmtpClient(smtpHost, smtpPort)
				{
					Credentials = new NetworkCredential(smtpUsername, smtpPassword),
					EnableSsl = enableSsl,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					UseDefaultCredentials = false
				};

				_logger.LogInformation("Sending password reset email to: {Email}", email);
				await smtpClient.SendMailAsync(mailMessage);
				_logger.LogInformation("Password reset email sent successfully to: {Email}", email);
			}
			catch (SmtpException ex)
			{
				_logger.LogError(ex, "SMTP error sending password reset email to: {Email}. Error: {Error}", email, ex.Message);
				throw new InvalidOperationException($"E-posta gönderilirken hata oluştu: {ex.Message}", ex);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "General error sending password reset email to: {Email}", email);
				throw;
			}
		}


	}
}
