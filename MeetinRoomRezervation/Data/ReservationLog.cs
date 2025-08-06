using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MeetinRoomRezervation.Data
{
    public class ReservationLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("reservationId")]
        public string ReservationId { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("userEmail")]
        public string UserEmail { get; set; } = string.Empty;

        [BsonElement("userName")]
        public string UserName { get; set; } = string.Empty;

        [BsonElement("userCompany")]
        public string UserCompany { get; set; } = string.Empty;

        [BsonElement("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [BsonElement("roomName")]
        public string RoomName { get; set; } = string.Empty;

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("action")]
        public string Action { get; set; } = string.Empty; // Created, Cancelled, Updated

        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("startTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

        [BsonElement("endTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime EndTime { get; set; }

        [BsonElement("performedBy")]
        public string PerformedBy { get; set; } = string.Empty;

        [BsonElement("performedByEmail")]
        public string PerformedByEmail { get; set; } = string.Empty;

        [BsonElement("details")]
        public string Details { get; set; } = string.Empty;
    }
}