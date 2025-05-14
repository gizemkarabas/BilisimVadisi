using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MeetinRoomRezervation.Models
{
	public class UserDto
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; } = string.Empty;
		[BsonElement("email")]
		public string Email { get; set; }
		[BsonElement("passwordHash")]
		public string PasswordHash { get; set; }

		[BsonElement("firstName")]
		public string FirstName { get; set; }

		[BsonElement("lastName")]
		public string LastName { get; set; }

		[BsonElement("company")]
		public string Company { get; set; } = string.Empty;

		[BsonElement("companyOfficial")]
		public string CompanyOfficial { get; set; } = string.Empty;

		[BsonElement("contactPhone")]
		public string ContactPhone { get; set; } = string.Empty;

		[BsonElement("monthlyUsageLimit")]
		public int MonthlyUsageLimit { get; set; }

		[BsonElement("usedThisMonth")]
		public int UsedThisMonth { get; set; } = 0;

		[BsonElement("isActive")]
		public bool IsActive { get; set; } = true;


	}

}
