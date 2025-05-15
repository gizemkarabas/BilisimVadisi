namespace MeetinRoomRezervation.Data
{
    public class MeetingRoom
    {
        public required string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public int Capacity { get; set; }
        public double OccupancyRate { get; set; }
        public bool IsAvailable { get; set; }
    }

}
