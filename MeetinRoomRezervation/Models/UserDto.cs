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
		public string Password { get; set; }

		public string ConfirmPassword { get; set; }

		[BsonElement("firstName")]
		public string FirstName { get; set; }

		[BsonElement("lastName")]
		public string LastName { get; set; }

		[BsonElement("company")]
		public string Company { get; set; }

		[BsonElement("companyOfficial")]
		public string CompanyOfficial { get; set; }

		[BsonElement("contactPhone")]
		public string ContactPhone { get; set; }

		[BsonElement("monthlyUsageLimit")]
		public int MonthlyUsageLimit { get; set; }

		[BsonElement("usedThisMonth")]
		public int UsedThisMonth { get; set; }

		[BsonElement("isActive")]
		public bool IsActive { get; set; } = true;
		public string? ResetToken { get; set; }
		public DateTime? ResetTokenExpiry { get; set; }

	}

}
