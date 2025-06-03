using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
using Microsoft.AspNetCore.Components.Authorization;
using MongoDB.Driver;
using System.Security.Claims;

namespace MeetinRoomRezervation.Services.ReservationService
{
	public class ReservationService : IReservationService
	{
		private readonly MongoDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly AuthenticationStateProvider _authStateProvider;
		private readonly ILogger<ReservationService> _logger;

		public ReservationService(
			MongoDbContext context,
			IHttpContextAccessor httpContextAccessor,
			AuthenticationStateProvider authStateProvider,
			ILogger<ReservationService> logger)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_authStateProvider = authStateProvider;
			_logger = logger;
		}

		public async Task<string> AddReservationAsync(ReservationDto reservationDto)
		{
			try
			{
				_logger.LogInformation("AddReservationAsync called for RoomId: {RoomId}", reservationDto.RoomId);
				_logger.LogInformation("Selected date: {Date}", reservationDto.SelectedDate);
				_logger.LogInformation("Selected slots count: {Count}", reservationDto.SelectedSlots?.Count ?? 0);

				if (reservationDto.SelectedSlots == null || !reservationDto.SelectedSlots.Any())
				{
					throw new InvalidOperationException("Hiç slot seçilmemiş.");
				}

				var currentUser = await GetCurrentUserAsync();
				if (currentUser == null)
				{
					_logger.LogWarning("Current user not found");
					throw new InvalidOperationException("Kullanıcı oturumu bulunamadı. Lütfen tekrar giriş yapın.");
				}

				var reservationIds = new List<string>();

				foreach (var slot in reservationDto.SelectedSlots)
				{
					_logger.LogInformation("Processing slot: {StartTime} - {EndTime}", slot.StartTime, slot.EndTime);

					// Seçilen tarihi kullanarak doğru DateTime oluştur
					var localStartTime = new DateTime(
						reservationDto.SelectedDate.Year,
						reservationDto.SelectedDate.Month,
						reservationDto.SelectedDate.Day,
						slot.StartTime.Hour,
						slot.StartTime.Minute,
						slot.StartTime.Second,
						DateTimeKind.Local
					);

					var localEndTime = new DateTime(
						reservationDto.SelectedDate.Year,
						reservationDto.SelectedDate.Month,
						reservationDto.SelectedDate.Day,
						slot.EndTime.Hour,
						slot.EndTime.Minute,
						slot.EndTime.Second,
						DateTimeKind.Local
					);

					// UTC'ye çevir
					var utcStartTime = localStartTime.ToUniversalTime();
					var utcEndTime = localEndTime.ToUniversalTime();

					_logger.LogInformation("Local time: {LocalStart} - {LocalEnd}", localStartTime, localEndTime);
					_logger.LogInformation("UTC time: {UtcStart} - {UtcEnd}", utcStartTime, utcEndTime);

					var reservation = new Reservation
					{
						Id = Guid.NewGuid().ToString(),
						UserId = currentUser.Id,
						RoomId = reservationDto.RoomId,
						StartTime = utcStartTime,
						EndTime = utcEndTime,
						CreatedAt = DateTime.UtcNow,
						Status = ReservationStatus.Active,

						User = new UserDto
						{
							Id = currentUser.Id,
							Email = currentUser.Email,
							Company = currentUser.Company ?? "",
							CompanyOfficial = currentUser.CompanyOfficial ?? "",
							ContactPhone = currentUser.ContactPhone ?? "",
							FirstName = currentUser.FirstName ?? "",
							LastName = currentUser.LastName ?? ""
						},

						Room = reservationDto.Room ?? new MeetingRoomDto(),
						Location = reservationDto.Location ?? ""
					};

					await _context.Reservations.InsertOneAsync(reservation);
					reservationIds.Add(reservation.Id);

					_logger.LogInformation("Reservation created: {ReservationId} - Local: {LocalStart}-{LocalEnd}, UTC: {UtcStart}-{UtcEnd}",
						reservation.Id, localStartTime, localEndTime, utcStartTime, utcEndTime);
				}

				_logger.LogInformation("Total reservations created: {Count}", reservationIds.Count);
				return string.Join(",", reservationIds);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating reservation for RoomId: {RoomId}", reservationDto.RoomId);
				throw;
			}
		}

		public async Task<bool> UpdateReservationAsync(ReservationDto updated)
		{
			var reservation = new Reservation
			{
				Id = updated.Id,
				UserId = updated.UserId,
				RoomId = updated.RoomId,
				StartTime = updated.StartTime,
				EndTime = updated.EndTime,

			};
			var filter = Builders<Reservation>.Filter.Eq(r => r.Id, updated.Id);
			var update = Builders<Reservation>.Update
				.Set(r => r.StartTime, updated.StartTime)
				.Set(r => r.EndTime, updated.EndTime);

			var result = await _context.Reservations.UpdateOneAsync(filter, update);
			return result.ModifiedCount > 0;
		}
		public async Task CancelReservationAsync(string reservationId)
		{
			var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservationId);
			await _context.Reservations.DeleteOneAsync(filter);
		}
		public async Task<List<ReservationDto>> GetAllReservationsAsync()
		{
			var reservations = await _context.Reservations.Find(_ => true).ToListAsync();
			var result = new List<ReservationDto>();
			foreach (var reservation in reservations)
			{
				string userEmail = null;
				if (!string.IsNullOrEmpty(reservation.UserId))
				{
					var userFilter = Builders<User>.Filter.Eq("_id", reservation.UserId);
					var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();
					userEmail = user?.Email;
				}

				string roomName = null;
				if (!string.IsNullOrEmpty(reservation.RoomId))
				{
					var roomFilter = Builders<Data.MeetingRoom>.Filter.Eq("_id", reservation.RoomId);
					var room = await _context.Rooms.Find(roomFilter).FirstOrDefaultAsync();
					roomName = room?.Name;
				}

				result.Add(new ReservationDto
				{
					Id = reservation.Id,
					UserId = reservation.UserId,
					RoomId = reservation.RoomId,
					UserEmail = userEmail,
					RoomName = roomName,
					StartTime = reservation.StartTime,
					EndTime = reservation.EndTime
				});
			}

			return result;

		}
		public async Task<List<ReservationDto>> GetReservationsByDateAsync(DateTime date)
		{
			try
			{
				// Seçilen tarihin başlangıcı ve bitişi (local time)
				var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
				var endOfDay = startOfDay.AddDays(1);

				// UTC'ye çevir
				var utcStartOfDay = startOfDay.ToUniversalTime();
				var utcEndOfDay = endOfDay.ToUniversalTime();

				var reservations = await _context.Reservations
					.Find(r => r.StartTime >= utcStartOfDay &&
							  r.StartTime < utcEndOfDay &&
							  r.Status == ReservationStatus.Active)
					.ToListAsync();

				return reservations.Select(r => new ReservationDto
				{
					Id = r.Id,
					RoomId = r.RoomId,
					StartTime = r.StartTime,
					EndTime = r.EndTime,
					User = r.User,
					Room = r.Room,
					Location = r.Location,
					SelectedDate = r.StartTime.ToLocalTime().Date
				}).ToList();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting reservations for date: {Date}", date);
				return new List<ReservationDto>();
			}
		}

		public async Task<List<ReservationDto>> GetUserReservationsAsync()
		{
			try
			{
				var currentUser = await GetCurrentUserAsync();
				if (currentUser == null)
				{
					_logger.LogWarning("Current user is null in GetUserReservationsAsync");
					return new List<ReservationDto>();
				}

				_logger.LogInformation("Getting reservations for user: {UserId}", currentUser.Id);

				var reservations = await _context.Reservations
					.Find(r => r.UserId == currentUser.Id && r.Status == ReservationStatus.Active)
					.SortByDescending(r => r.StartTime)
					.ToListAsync();

				_logger.LogInformation("Found {Count} reservations for user {UserId}", reservations.Count, currentUser.Id);

				var result = new List<ReservationDto>();

				foreach (var reservation in reservations)
				{
					// MongoDB'den gelen zamanları UTC olarak işaretle
					var utcStartTime = DateTime.SpecifyKind(reservation.StartTime, DateTimeKind.Utc);
					var utcEndTime = DateTime.SpecifyKind(reservation.EndTime, DateTimeKind.Utc);

					_logger.LogInformation("Processing reservation: {Id}, StartTime: {StartTime} UTC, EndTime: {EndTime} UTC",
						reservation.Id, utcStartTime, utcEndTime);

					// Room bilgilerini al
					string roomName = reservation.Room?.Name ?? "";
					if (string.IsNullOrEmpty(roomName) && !string.IsNullOrEmpty(reservation.RoomId))
					{
						var room = await _context.Rooms.Find(r => r.Id == reservation.RoomId).FirstOrDefaultAsync();
						roomName = room?.Name ?? "Bilinmeyen Oda";
					}

					var dto = new ReservationDto
					{
						Id = reservation.Id,
						UserId = reservation.UserId,
						RoomId = reservation.RoomId,
						StartTime = utcStartTime,  // UTC olarak döndür
						EndTime = utcEndTime,      // UTC olarak döndür
						User = reservation.User,
						Room = reservation.Room ?? new MeetingRoomDto { Name = roomName },
						RoomName = roomName,
						Location = reservation.Location,
						SelectedDate = utcStartTime.ToLocalTime().Date // Local date için
					};

					result.Add(dto);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting user reservations");
				return new List<ReservationDto>();
			}
		}

		public async Task<bool> DeleteReservationAsync(string reservationId)
		{
			try
			{
				var currentUser = await GetCurrentUserAsync();
				if (currentUser == null)
				{
					return false;
				}

				// Kullanıcının kendi rezervasyonunu sildiğinden emin ol
				var reservation = await _context.Reservations
					.Find(r => r.Id == reservationId && r.UserId == currentUser.Id)
					.FirstOrDefaultAsync();

				if (reservation == null)
				{
					_logger.LogWarning("Reservation not found or user not authorized: {ReservationId}", reservationId);
					return false;
				}

				// Soft delete - status'u cancelled yap
				var update = Builders<Reservation>.Update.Set(r => r.Status, ReservationStatus.Cancelled);
				var result = await _context.Reservations.UpdateOneAsync(r => r.Id == reservationId, update);

				_logger.LogInformation("Reservation cancelled: {ReservationId} by User: {UserId}",
					reservationId, currentUser.Id);

				return result.ModifiedCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting reservation: {ReservationId}", reservationId);
				return false;
			}
		}

		public async Task<bool> AdminDeleteReservationAsync(string reservationId)
		{
			try
			{
				var update = Builders<Reservation>.Update.Set(r => r.Status, ReservationStatus.Cancelled);
				var result = await _context.Reservations.UpdateOneAsync(r => r.Id == reservationId, update);

				_logger.LogInformation("Reservation cancelled by admin: {ReservationId}", reservationId);
				return result.ModifiedCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error admin deleting reservation: {ReservationId}", reservationId);
				return false;
			}
		}
		public async Task<User?> GetCurrentUserAsync()
		{
			try
			{
				// Önce HttpContext'ten dene
				var httpContext = _httpContextAccessor.HttpContext;
				if (httpContext?.User?.Identity?.IsAuthenticated == true)
				{
					var userEmail = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
					var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

					_logger.LogInformation("HttpContext - Email: {Email}, UserId: {UserId}", userEmail, userId);

					// Email ile kullanıcıyı bul
					if (!string.IsNullOrEmpty(userEmail))
					{
						var foundUser = await _context.Users
							.Find(u => u.Email == userEmail)
							.FirstOrDefaultAsync();

						if (foundUser != null)
						{
							_logger.LogInformation("User found by email: {UserId}", foundUser.Id);
							return foundUser;
						}
					}

					// UserId ile kullanıcıyı bul
					if (!string.IsNullOrEmpty(userId))
					{
						var foundUser = await _context.Users
							.Find(u => u.Id == userId)
							.FirstOrDefaultAsync();

						if (foundUser != null)
						{
							_logger.LogInformation("User found by ID: {UserId}", foundUser.Id);
							return foundUser;
						}
					}
				}

				// AuthenticationStateProvider'dan dene
				var authState = await _authStateProvider.GetAuthenticationStateAsync();
				var user = authState.User;

				_logger.LogInformation("AuthState - IsAuthenticated: {IsAuthenticated}", user.Identity?.IsAuthenticated);

				if (user.Identity?.IsAuthenticated == true)
				{
					var userEmail = user.FindFirst(ClaimTypes.Name)?.Value;
					var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

					_logger.LogInformation("AuthState - Email: {Email}, UserId: {UserId}", userEmail, userId);

					// Email ile kullanıcıyı bul
					if (!string.IsNullOrEmpty(userEmail))
					{
						var foundUser = await _context.Users
							.Find(u => u.Email == userEmail)
							.FirstOrDefaultAsync();

						if (foundUser != null)
						{
							_logger.LogInformation("User found by email from AuthState: {UserId}", foundUser.Id);
							return foundUser;
						}
					}

					// UserId ile kullanıcıyı bul
					if (!string.IsNullOrEmpty(userId))
					{
						var foundUser = await _context.Users
							.Find(u => u.Id == userId)
							.FirstOrDefaultAsync();

						if (foundUser != null)
						{
							_logger.LogInformation("User found by ID from AuthState: {UserId}", foundUser.Id);
							return foundUser;
						}
					}
				}

				_logger.LogWarning("User not found in any method");
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting current user");
				return null;
			}
		}


	}
}
