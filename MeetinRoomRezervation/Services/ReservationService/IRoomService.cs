using MeetinRoomRezervation.Models;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public interface IRoomService
	{
		//Task<List<MeetingRoomDto>> GetAllRoomsAsync();
		//Task<MeetingRoomDto?> GetRoomByIdAsync(string roomId);
		Task UpdateRoomAsync(MeetingRoomDto roomDto);
		Task DeleteRoomAsync(string roomId);

	}

}
