using MongoDB.Driver;

namespace MeetinRoomRezervation.Data
{
    public class MongoDbContext(IMongoClient mongoClient)
    {
        public IMongoCollection<User> Users => mongoClient.GetDatabase("ReservationDb").GetCollection<User>("Users");
        public IMongoCollection<Reservation> Reservations => mongoClient.GetDatabase("ReservationDb").GetCollection<Reservation>("Reservations");
        public IMongoCollection<MeetingRoom> Rooms => mongoClient.GetDatabase("ReservationDb").GetCollection<MeetingRoom>("MeetingRooms");

    }
}