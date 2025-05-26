namespace MeetinRoomRezervation.Data
{
	public class User
	{
		public string Id { get; set; }
		public required string Email { get; set; }
		public string PasswordHash { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Company { get; set; }
		public string CompanyOfficial { get; set; }
		public string ContactPhone { get; set; } = string.Empty;
		public int MonthlyUsageLimit { get; set; }
		public int UsedThisMonth { get; set; } = 0;
		public UserRole Role { get; set; } = UserRole.User;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public bool IsActive { get; set; } = true;
	}

}
