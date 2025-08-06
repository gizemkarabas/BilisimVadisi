using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
using MongoDB.Driver;

namespace MeetinRoomRezervation.Services.LogService
{
    public class ReservationLogService : IReservationLogService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<ReservationLogService> _logger;

        public ReservationLogService(MongoDbContext context, ILogger<ReservationLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogReservationActionAsync(string reservationId, string userId, string userEmail,
            string userName, string userCompany, string roomId, string roomName, string location,
            string action, DateTime startTime, DateTime endTime, string performedBy,
            string performedByEmail, string details = "")
        {
            try
            {
                var log = new ReservationLog
                {
                    Id = Guid.NewGuid().ToString(),
                    ReservationId = reservationId,
                    UserId = userId,
                    UserEmail = userEmail,
                    UserName = userName,
                    UserCompany = userCompany,
                    RoomId = roomId,
                    RoomName = roomName,
                    Location = location,
                    Action = action,
                    Timestamp = DateTime.UtcNow,
                    StartTime = startTime,
                    EndTime = endTime,
                    PerformedBy = performedBy,
                    PerformedByEmail = performedByEmail,
                    Details = details
                };

                await _context.ReservationLogs.InsertOneAsync(log);
                _logger.LogInformation("Reservation log created for action: {Action}, ReservationId: {ReservationId}", action, reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation log for action: {Action}, ReservationId: {ReservationId}", action, reservationId);
            }
        }

        public async Task<List<ReservationLogDto>> GetAllLogsAsync()
        {
            try
            {
                var logs = await _context.ReservationLogs
                    .Find(_ => true)
                    .SortByDescending(l => l.Timestamp)
                    .ToListAsync();

                return logs.Select(log => new ReservationLogDto
                {
                    Id = log.Id,
                    ReservationId = log.ReservationId,
                    UserId = log.UserId,
                    UserEmail = log.UserEmail,
                    UserName = log.UserName,
                    UserCompany = log.UserCompany,
                    RoomId = log.RoomId,
                    RoomName = log.RoomName,
                    Location = log.Location,
                    Action = log.Action,
                    Timestamp = log.Timestamp,
                    StartTime = log.StartTime,
                    EndTime = log.EndTime,
                    PerformedBy = log.PerformedBy,
                    PerformedByEmail = log.PerformedByEmail,
                    Details = log.Details
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all reservation logs");
                return new List<ReservationLogDto>();
            }
        }

        public async Task<List<ReservationLogDto>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var filter = Builders<ReservationLog>.Filter.And(
                    Builders<ReservationLog>.Filter.Gte(l => l.Timestamp, startDate),
                    Builders<ReservationLog>.Filter.Lte(l => l.Timestamp, endDate)
                );

                var logs = await _context.ReservationLogs
                    .Find(filter)
                    .SortByDescending(l => l.Timestamp)
                    .ToListAsync();

                return logs.Select(log => new ReservationLogDto
                {
                    Id = log.Id,
                    ReservationId = log.ReservationId,
                    UserId = log.UserId,
                    UserEmail = log.UserEmail,
                    UserName = log.UserName,
                    UserCompany = log.UserCompany,
                    RoomId = log.RoomId,
                    RoomName = log.RoomName,
                    Location = log.Location,
                    Action = log.Action,
                    Timestamp = log.Timestamp,
                    StartTime = log.StartTime,
                    EndTime = log.EndTime,
                    PerformedBy = log.PerformedBy,
                    PerformedByEmail = log.PerformedByEmail,
                    Details = log.Details
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by date range");
                return new List<ReservationLogDto>();
            }
        }

        public async Task<List<ReservationLogDto>> GetLogsByUserAsync(string userId)
        {
            try
            {
                var filter = Builders<ReservationLog>.Filter.Eq(l => l.UserId, userId);
                var logs = await _context.ReservationLogs
                    .Find(filter)
                    .SortByDescending(l => l.Timestamp)
                    .ToListAsync();

                return logs.Select(log => new ReservationLogDto
                {
                    Id = log.Id,
                    ReservationId = log.ReservationId,
                    UserId = log.UserId,
                    UserEmail = log.UserEmail,
                    UserName = log.UserName,
                    UserCompany = log.UserCompany,
                    RoomId = log.RoomId,
                    RoomName = log.RoomName,
                    Location = log.Location,
                    Action = log.Action,
                    Timestamp = log.Timestamp,
                    StartTime = log.StartTime,
                    EndTime = log.EndTime,
                    PerformedBy = log.PerformedBy,
                    PerformedByEmail = log.PerformedByEmail,
                    Details = log.Details
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by user");
                return new List<ReservationLogDto>();
            }
        }

        public async Task<List<ReservationLogDto>> GetLogsByRoomAsync(string roomId)
        {
            try
            {
                var filter = Builders<ReservationLog>.Filter.Eq(l => l.RoomId, roomId);
                var logs = await _context.ReservationLogs
                    .Find(filter)
                    .SortByDescending(l => l.Timestamp)
                    .ToListAsync();

                return logs.Select(log => new ReservationLogDto
                {
                    Id = log.Id,
                    ReservationId = log.ReservationId,
                    UserId = log.UserId,
                    UserEmail = log.UserEmail,
                    UserName = log.UserName,
                    UserCompany = log.UserCompany,
                    RoomId = log.RoomId,
                    RoomName = log.RoomName,
                    Location = log.Location,
                    Action = log.Action,
                    Timestamp = log.Timestamp,
                    StartTime = log.StartTime,
                    EndTime = log.EndTime,
                    PerformedBy = log.PerformedBy,
                    PerformedByEmail = log.PerformedByEmail,
                    Details = log.Details
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by room");
                return new List<ReservationLogDto>();
            }
        }
    }
}
