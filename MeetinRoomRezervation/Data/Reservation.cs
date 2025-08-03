using MeetinRoomRezervation.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MeetinRoomRezervation.Data
{
    public class Reservation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public ReservationStatus Status { get; set; }
        public UserDto User { get; set; } = new();
        public MeetingRoomDto Room { get; set; } = new();
        public string Location { get; set; }
        public bool ReminderSent { get; set; }
        public string MeetingTitle { get; set; }
    }

    public enum ReservationStatus
    {
        Active,
        Pending,
        Confirmed,
        Cancelled,
        Completed

    }
}

