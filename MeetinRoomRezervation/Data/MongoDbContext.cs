using MeetinRoomRezervation.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

public class MongoDbContext
{
	private readonly IMongoDatabase _database;
	public MongoDbContext(IConfiguration configuration)
	{
		var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
		_database = client.GetDatabase("ReservationDb");

		BsonSerializer.RegisterSerializer(new ObjectIdSerializer());
		BsonClassMap.RegisterClassMap<User>(cm =>
		{
			cm.AutoMap();
			cm.MapIdMember(c => c.Id).SetSerializer(new StringSerializer(BsonType.ObjectId));
		});

		var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
		ConventionRegistry.Register("camelCase", conventionPack, t => true);

	}
	public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
	public IMongoCollection<Reservation> Reservations => _database.GetCollection<Reservation>("Reservations");
	public IMongoCollection<MeetingRoom> Rooms => _database.GetCollection<MeetingRoom>("MeetingRooms");

}
