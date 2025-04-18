using MeetinRoomRezervation.Models;
using MongoDB.Driver;

public class MongoDbContext
{
	private readonly IMongoDatabase _database;

	public MongoDbContext(IConfiguration configuration)
	{
		var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
		_database = client.GetDatabase("ReservationDb");
	}

	public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
	public IMongoCollection<Reservation> Reservations => _database.GetCollection<Reservation>("Reservations");
	public IMongoCollection<MeetingRoom> Rooms => _database.GetCollection<MeetingRoom>("MeetingRooms");

}
