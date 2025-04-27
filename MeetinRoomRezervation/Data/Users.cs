	using MongoDB.Bson;
	using MongoDB.Bson.Serialization.Attributes;


	namespace MeetinRoomRezervation.Data
{
		public class User
		{
			
			public string? Id { get; set; }

			public required string Email { get; set; }
			
			public required string PasswordHash { get; set; } 
		}

	}
