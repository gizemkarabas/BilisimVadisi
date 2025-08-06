using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.LogService
{
    public interface IReservationLogService
    {
        Task LogReservationActionAsync(string reservationId, string userId, string userEmail, 
            string userName, string userCompany, string roomId, string roomName, string location,
            string action, DateTime startTime, DateTime endTime, string performedBy, 
            string performedByEmail, string details = "");
        
        Task<List<ReservationLogDto>> GetAllLogsAsync();
        Task<List<ReservationLogDto>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<ReservationLogDto>> GetLogsByUserAsync(string userId);
        Task<List<ReservationLogDto>> GetLogsByRoomAsync(string roomId);
    }
}
