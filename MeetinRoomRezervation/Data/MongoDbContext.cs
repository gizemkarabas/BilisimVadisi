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

	public IMongoCollection<UserDto> Users => _database.GetCollection<UserDto>("Users");
	public IMongoCollection<ReservationDto> Reservations => _database.GetCollection<ReservationDto>("Reservations");
	public IMongoCollection<MeetingRoomDto> Rooms => _database.GetCollection<MeetingRoomDto>("MeetingRooms");

}
