using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public interface IRoomService
	{
		Task<List<MeetingRoomDto>> GetAllRoomsAsync();
		Task<MeetingRoomDto?> GetRoomByIdAsync(string roomId);
		Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date);
		Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(string roomId, DateTime date);

		Task<List<SlotDto>> GetSlotsWithStatusAsync(string roomId, DateTime date);
		Task<bool> AddRoomAsync(MeetingRoomDto roomDto);
		Task UpdateRoomAsync(MeetingRoomDto roomDto);
		Task DeleteRoomAsync(string roomId);

	}

}
