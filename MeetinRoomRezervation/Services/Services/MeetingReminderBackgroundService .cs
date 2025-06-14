using MeetinRoomRezervation.Services.Services;

namespace MeetinRoomRezervation.Services.BackgroundServices
{
	public class MeetingReminderBackgroundService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<MeetingReminderBackgroundService> _logger;
		private readonly TimeSpan _period = TimeSpan.FromMinutes(1); // Her dakika kontrol et

		public MeetingReminderBackgroundService(
			IServiceProvider serviceProvider,
			ILogger<MeetingReminderBackgroundService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var reminderService = scope.ServiceProvider.GetRequiredService<IMeetingReminderService>();

					await reminderService.CheckAndSendRemindersAsync();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in MeetingReminderBackgroundService");
				}

				await Task.Delay(_period, stoppingToken);
			}
		}
	}
}
