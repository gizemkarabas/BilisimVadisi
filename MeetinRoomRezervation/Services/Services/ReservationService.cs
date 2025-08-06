using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Models;
using MeetinRoomRezervation.Services.LogService;
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
        private readonly IUserService _userService;
        private readonly IReservationLogService _logService;

        public ReservationService(
            MongoDbContext context,
            IHttpContextAccessor httpContextAccessor,
            AuthenticationStateProvider authStateProvider,
            ILogger<ReservationService> logger, 
            IUserService userService,
            IReservationLogService logService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _authStateProvider = authStateProvider;
            _logger = logger;
            _userService = userService;
            _logService = logService;
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

                // Admin seçimi varsa, rezervasyonu seçilen kullanıcıya ata
                UserDto targetUser = null;
                if (!string.IsNullOrEmpty(reservationDto.UserId))
                {
                    targetUser = reservationDto.User;
                    if (targetUser == null)
                    {
                        var user = await _userService.GetUserByIdAsync(reservationDto.UserId);
                        if (user != null)
                        {
                            targetUser = new UserDto
                            {
                                Id = user.Id,
                                Email = user.Email,
                                Company = user.Company,
                                CompanyOfficial = user.CompanyOfficial,
                                ContactPhone = user.ContactPhone,
                                FirstName = user.FirstName,
                                LastName = user.LastName
                            };
                        }
                    }
                }
                else
                {
                    var user = await GetCurrentUserAsync();
                    if (user != null)
                    {
                        targetUser = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Company = user.Company,
                            CompanyOfficial = user.CompanyOfficial,
                            ContactPhone = user.ContactPhone,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        };
                    }
                }
                if (targetUser == null)
                {
                    _logger.LogWarning("Target user not found");
                    throw new InvalidOperationException("Kullanıcı bulunamadı. Lütfen tekrar deneyin.");
                }


                var mergedSlots = MergeConsecutiveSlots(reservationDto.SelectedSlots.ToList());
                var reservationIds = new List<string>();

                // Room ve User detaylarını veritabanından çek
                MeetingRoomDto? roomDto = null;
                if (!string.IsNullOrEmpty(reservationDto.RoomId))
                {
                    var room = await _context.Rooms.Find(r => r.Id == reservationDto.RoomId).FirstOrDefaultAsync();
                    if (room != null)
                    {
                        roomDto = new MeetingRoomDto
                        {
                            Id = room.Id,
                            Name = room.Name,
                            Location = room.Location,
                            Capacity = room.Capacity,
                        };
                    }
                }

                UserDto? userDto = null;
                if (!string.IsNullOrEmpty(targetUser?.Id))
                {
                    var user = await _context.Users.Find(u => u.Id == targetUser.Id).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        userDto = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Company = user.Company,
                            CompanyOfficial = user.CompanyOfficial,
                            ContactPhone = user.ContactPhone,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        };
                    }
                }

                foreach (var mergedSlot in mergedSlots)
                {
                    _logger.LogInformation("Processing merged slot: {StartTime} - {EndTime}", mergedSlot.StartTime, mergedSlot.EndTime);

                    var localStartTime = new DateTime(
                        reservationDto.SelectedDate.Year,
                        reservationDto.SelectedDate.Month,
                        reservationDto.SelectedDate.Day,
                        mergedSlot.StartTime.Hour,
                        mergedSlot.StartTime.Minute,
                        mergedSlot.StartTime.Second,
                        DateTimeKind.Local
                    );

                    var localEndTime = new DateTime(
                        reservationDto.SelectedDate.Year,
                        reservationDto.SelectedDate.Month,
                        reservationDto.SelectedDate.Day,
                        mergedSlot.EndTime.Hour,
                        mergedSlot.EndTime.Minute,
                        mergedSlot.EndTime.Second,
                        DateTimeKind.Local
                    );

                    if (localEndTime.Hour == 0)
                    {
                        localEndTime = localEndTime.AddDays(1).AddMinutes(-1);
                    }

                    // UTC'ye çevir
                    var utcStartTime = localStartTime.ToUniversalTime();
                    var utcEndTime = localEndTime.ToUniversalTime();

                    _logger.LogInformation("Local time: {LocalStart} - {LocalEnd}", localStartTime, localEndTime);
                    _logger.LogInformation("UTC time: {UtcStart} - {UtcEnd}", utcStartTime, utcEndTime);

                    var reservation = new Reservation
                    {
                        UserId = targetUser.Id,
                        RoomId = reservationDto.RoomId,
                        StartTime = utcStartTime,
                        EndTime = utcEndTime,
                        CreatedAt = DateTime.UtcNow,
                        Status = ReservationStatus.Active,
                        User = userDto,
                        Room = roomDto,
                        // Öncelik: reservationDto.Location (summary'den gelen), sonra roomDto.Location, en son ""
                        Location = !string.IsNullOrWhiteSpace(reservationDto.Location) ? reservationDto.Location : (roomDto?.Location ?? "")
                    };
                    // Rezervasyon oluşturulmadan önce kontrol
                    var reservationHours = (int)Math.Ceiling((reservation.EndTime - reservation.StartTime).TotalHours);
                    var canMakeReservation = await _userService.CanUserMakeReservationAsync(reservation.UserId, reservationHours);

                    if (!canMakeReservation)
                    {
                        throw new InvalidOperationException("Aylık kullanım limitinizi aştınız. Rezervasyon yapılamaz.");
                    }
                    await _context.Reservations.InsertOneAsync(reservation);
                    reservationIds.Add(reservation.Id);

                    // Log kaydı oluştur
                    try
                    {
                        // İşlemi yapan kişiyi belirle (admin vs normal user)
                        var currentUser = await GetCurrentUserAsync();
                        var performedByUser = currentUser?.Id ?? "";
                        var performedByEmail = currentUser?.Email ?? "";
                        
                        // Eğer admin başka kullanıcı adına rezervasyon yapıyorsa detayları güncelle
                        var details = "Rezervasyon oluşturuldu";
                        if (!string.IsNullOrEmpty(reservationDto.UserId) && currentUser?.Id != targetUser?.Id)
                        {
                            details = $"Admin tarafından {targetUser?.FirstName} {targetUser?.LastName} adına rezervasyon oluşturuldu";
                        }

                        await _logService.LogReservationActionAsync(
                            reservationId: reservation.Id,
                            userId: targetUser?.Id ?? "",
                            userEmail: targetUser?.Email ?? "",
                            userName: $"{targetUser?.FirstName} {targetUser?.LastName}".Trim(),
                            userCompany: targetUser?.Company ?? "",
                            roomId: reservation.RoomId,
                            roomName: roomDto?.Name ?? "",
                            location: reservation.Location,
                            action: "Created",
                            startTime: reservation.StartTime,
                            endTime: reservation.EndTime,
                            performedBy: performedByUser,
                            performedByEmail: performedByEmail,
                            details: details
                        );
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to log reservation creation for ReservationId: {ReservationId}", reservation.Id);
                    }

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
            try
            {
                // Mevcut rezervasyon bilgilerini al
                var existingReservation = await _context.Reservations
                    .Find(r => r.Id == updated.Id)
                    .FirstOrDefaultAsync();

                if (existingReservation == null)
                {
                    return false;
                }

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

                if (result.ModifiedCount > 0)
                {
                    // Log kaydı oluştur
                    try
                    {
                        var user = await _userService.GetUserByIdAsync(existingReservation.UserId);
                        var room = await _context.Rooms
                            .Find(r => r.Id == existingReservation.RoomId)
                            .FirstOrDefaultAsync();
                        var currentUser = await GetCurrentUserAsync();

                        // Eğer admin başka kullanıcının rezervasyonunu güncelliyorsa detayları güncelle
                        var details = $"Rezervasyon güncellendi - Yeni zaman: {updated.StartTime:dd.MM.yyyy HH:mm} - {updated.EndTime:dd.MM.yyyy HH:mm}";
                        if (currentUser?.Id != user?.Id)
                        {
                            details = $"Admin tarafından {user?.FirstName} {user?.LastName} adına rezervasyon güncellendi - Yeni zaman: {updated.StartTime:dd.MM.yyyy HH:mm} - {updated.EndTime:dd.MM.yyyy HH:mm}";
                        }

                        await _logService.LogReservationActionAsync(
                            reservationId: updated.Id,
                            userId: user?.Id ?? "",
                            userEmail: user?.Email ?? "",
                            userName: $"{user?.FirstName} {user?.LastName}".Trim(),
                            userCompany: user?.Company ?? "",
                            roomId: existingReservation.RoomId,
                            roomName: room?.Name ?? "",
                            location: existingReservation.Location,
                            action: "Updated",
                            startTime: updated.StartTime,
                            endTime: updated.EndTime,
                            performedBy: currentUser?.Id ?? "",
                            performedByEmail: currentUser?.Email ?? "",
                            details: details
                        );
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to log reservation update for ReservationId: {ReservationId}", updated.Id);
                    }
                }

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", updated.Id);
                throw;
            }
        }
        public async Task CancelReservationAsync(string reservationId)
        {
            try
            {
                // Rezervasyon bilgilerini iptal etmeden önce al
                var reservation = await _context.Reservations
                    .Find(r => r.Id == reservationId)
                    .FirstOrDefaultAsync();

                if (reservation != null)
                {
                    // Kullanıcı ve salon bilgilerini al
                    var user = await _userService.GetUserByIdAsync(reservation.UserId);
                    var room = await _context.Rooms
                        .Find(r => r.Id == reservation.RoomId)
                        .FirstOrDefaultAsync();

                    // Mevcut kullanıcı bilgilerini al
                    var currentUser = await GetCurrentUserAsync();

                    // Rezervasyonu iptal et
                    var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservationId);
                    await _context.Reservations.DeleteOneAsync(filter);

                    // Log kaydı oluştur
                    try
                    {
                        // Eğer admin başka kullanıcının rezervasyonunu iptal ediyorsa detayları güncelle
                        var details = "Rezervasyon iptal edildi";
                        if (currentUser?.Id != user?.Id)
                        {
                            details = $"Admin tarafından {user?.FirstName} {user?.LastName} adına rezervasyon iptal edildi";
                        }

                        await _logService.LogReservationActionAsync(
                            reservationId: reservationId,
                            userId: user?.Id ?? "",
                            userEmail: user?.Email ?? "",
                            userName: $"{user?.FirstName} {user?.LastName}".Trim(),
                            userCompany: user?.Company ?? "",
                            roomId: reservation.RoomId,
                            roomName: room?.Name ?? "",
                            location: reservation.Location,
                            action: "Cancelled",
                            startTime: reservation.StartTime,
                            endTime: reservation.EndTime,
                            performedBy: currentUser?.Id ?? "",
                            performedByEmail: currentUser?.Email ?? "",
                            details: details
                        );
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to log reservation cancellation for ReservationId: {ReservationId}", reservationId);
                    }
                }
                else
                {
                    _logger.LogWarning("Reservation not found for cancellation: {ReservationId}", reservationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation: {ReservationId}", reservationId);
                throw;
            }
        }
        public async Task<List<ReservationDto>> GetAllReservationsAsync()
        {
            var reservations = await _context.Reservations
                .Find(r => r.Status == ReservationStatus.Active)
                .ToListAsync();
            var result = new List<ReservationDto>();
            foreach (var reservation in reservations)
            {
                UserDto userDto = null;
                string userEmail = null;
                if (!string.IsNullOrEmpty(reservation.UserId))
                {
                    var userFilter = Builders<User>.Filter.Eq(p => p.Id, reservation.UserId);
                    var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();
                    userEmail = user?.Email;
                    if (user != null)
                    {
                        userDto = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Company = user.Company,
                            CompanyOfficial = user.CompanyOfficial,
                            ContactPhone = user.ContactPhone,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        };
                    }
                }

                string roomName = null;
                MeetingRoomDto roomDto = null;
                if (!string.IsNullOrEmpty(reservation.RoomId))
                {
                    var roomFilter = Builders<Data.MeetingRoom>.Filter.Eq(p => p.Id, reservation.RoomId);
                    var room = await _context.Rooms.Find(roomFilter).FirstOrDefaultAsync();
                    roomName = room?.Name;
                    if (room != null)
                    {
                        roomDto = new MeetingRoomDto
                        {
                            Id = room.Id,
                            Name = room.Name,
                            Location = room.Location,
                            Capacity = room.Capacity,
                        };
                    }
                }

                result.Add(new ReservationDto
                {
                    Id = reservation.Id,
                    UserId = reservation.UserId,
                    RoomId = reservation.RoomId,
                    UserEmail = userEmail,
                    RoomName = roomName,
                    StartTime = reservation.StartTime,
                    EndTime = reservation.EndTime,
                    User = userDto,
                    Room = roomDto,
                    Location = reservation.Location,
                    Status = reservation.Status
                });
            }

            return result;
        }
        public async Task<List<ReservationDto>> GetReservationsByDateAsync(DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                var reservations = await _context.Reservations
                    .Find(r => r.StartTime >= startOfDay && r.StartTime < endOfDay)
                    .ToListAsync();

                var reservationDtos = new List<ReservationDto>();

                foreach (var reservation in reservations)
                {
                    // Eğer UserId null ise UserEmail'den user'ı bul
                    User user = null;
                    if (!string.IsNullOrEmpty(reservation.UserId))
                    {
                        user = await _context.Users
                            .Find(u => u.Id == reservation.UserId)
                            .FirstOrDefaultAsync();
                    }
                    else if (!string.IsNullOrEmpty(reservation.User.Email))
                    {
                        user = await _context.Users
                            .Find(u => u.Email == reservation.User.Email)
                            .FirstOrDefaultAsync();

                        // UserId'yi güncelle
                        if (user != null)
                        {
                            var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservation.Id);
                            var update = Builders<Reservation>.Update.Set(r => r.UserId, user.Id);
                            await _context.Reservations.UpdateOneAsync(filter, update);
                            reservation.UserId = user.Id;
                        }
                    }

                    var reservationDto = new ReservationDto
                    {
                        Id = reservation.Id,
                        UserId = reservation.UserId ?? user?.Id,
                        UserEmail = reservation.User.Email ?? user?.Email,
                        RoomId = reservation.RoomId,
                        StartTime = reservation.StartTime,
                        EndTime = reservation.EndTime,
                        CreateDate = reservation.CreatedAt,
                        User = user != null ? new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Company = user.Company,
                            CompanyOfficial = user.CompanyOfficial,
                            ContactPhone = user.ContactPhone,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        } : null,
                        Status = reservation.Status
                    };

                    reservationDtos.Add(reservationDto);
                }

                return reservationDtos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetReservationsByDateAsync: {ex.Message}");
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
                // Rezervasyon bilgilerini silmeden önce al
                var reservation = await _context.Reservations
                    .Find(r => r.Id == reservationId)
                    .FirstOrDefaultAsync();

                if (reservation != null)
                {
                    // Kullanıcı ve salon bilgilerini al
                    var user = await _userService.GetUserByIdAsync(reservation.UserId);
                    var room = await _context.Rooms
                        .Find(r => r.Id == reservation.RoomId)
                        .FirstOrDefaultAsync();

                    // Mevcut kullanıcı bilgilerini al (admin)
                    var currentUser = await GetCurrentUserAsync();

                    var update = Builders<Reservation>.Update.Set(r => r.Status, ReservationStatus.Cancelled);
                    var result = await _context.Reservations.UpdateOneAsync(r => r.Id == reservationId, update);

                    // Log kaydı oluştur
                    if (result.ModifiedCount > 0)
                    {
                        try
                        {
                            var details = $"Admin tarafından {user?.FirstName} {user?.LastName} adına rezervasyon silindi";

                            await _logService.LogReservationActionAsync(
                                reservationId: reservationId,
                                userId: user?.Id ?? "",
                                userEmail: user?.Email ?? "",
                                userName: $"{user?.FirstName} {user?.LastName}".Trim(),
                                userCompany: user?.Company ?? "",
                                roomId: reservation.RoomId,
                                roomName: room?.Name ?? "",
                                location: reservation.Location,
                                action: "Cancelled",
                                startTime: reservation.StartTime,
                                endTime: reservation.EndTime,
                                performedBy: currentUser?.Id ?? "",
                                performedByEmail: currentUser?.Email ?? "",
                                details: details
                            );
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogError(logEx, "Failed to log admin reservation deletion for ReservationId: {ReservationId}", reservationId);
                        }
                    }

                    _logger.LogInformation("Reservation cancelled by admin: {ReservationId}", reservationId);
                    return result.ModifiedCount > 0;
                }
                else
                {
                    _logger.LogWarning("Reservation not found for admin deletion: {ReservationId}", reservationId);
                    return false;
                }
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
                // Yardımcı fonksiyon: claim'leri sırayla dene
                string? GetEmail(ClaimsPrincipal principal)
                {
                    return principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst(ClaimTypes.Name)?.Value
                        ?? principal.FindFirst("email")?.Value;
                }
                string? GetUserId(ClaimsPrincipal principal)
                {
                    return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value
                        ?? principal.FindFirst("id")?.Value;
                }

                // Önce HttpContext'ten dene
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var userEmail = GetEmail(httpContext.User);
                    var userId = GetUserId(httpContext.User);

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
                    var userEmail = GetEmail(user);
                    var userId = GetUserId(user);

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

        private List<SlotDto> MergeConsecutiveSlots(List<SlotDto> slots)
        {
            if (slots == null || !slots.Any())
                return new List<SlotDto>();

            // Slotları başlangıç saatine göre sırala
            var sortedSlots = slots.OrderBy(s => s.StartTime).ToList();
            var mergedSlots = new List<SlotDto>();

            _logger.LogInformation("Starting merge process with {Count} slots", sortedSlots.Count);

            // İlk slotu başlangıç olarak al
            var currentMergedSlot = new SlotDto
            {
                StartTime = sortedSlots[0].StartTime,
                EndTime = sortedSlots[0].EndTime
            };

            for (int i = 1; i < sortedSlots.Count; i++)
            {
                var nextSlot = sortedSlots[i];

                _logger.LogInformation("Comparing current slot end time {CurrentEnd} with next slot start time {NextStart}",
                    currentMergedSlot.EndTime, nextSlot.StartTime);

                // Eğer mevcut slotun bitiş saati, bir sonraki slotun başlangıç saatine eşitse birleştir
                if (currentMergedSlot.EndTime == nextSlot.StartTime)
                {
                    // Mevcut slotun bitiş saatini güncelle
                    currentMergedSlot.EndTime = nextSlot.EndTime;

                    _logger.LogInformation("Merged slots: {StartTime} - {EndTime}",
                        currentMergedSlot.StartTime, currentMergedSlot.EndTime);
                }
                else
                {
                    // Ard arda değilse, mevcut slotu listeye ekle ve yeni slotu başlat
                    mergedSlots.Add(currentMergedSlot);

                    _logger.LogInformation("Added merged slot to list: {StartTime} - {EndTime}",
                        currentMergedSlot.StartTime, currentMergedSlot.EndTime);

                    currentMergedSlot = new SlotDto
                    {
                        StartTime = nextSlot.StartTime,
                        EndTime = nextSlot.EndTime
                    };
                }
            }

            // Son slotu da ekle
            mergedSlots.Add(currentMergedSlot);

            _logger.LogInformation("Final merged slot added: {StartTime} - {EndTime}",
                currentMergedSlot.StartTime, currentMergedSlot.EndTime);

            _logger.LogInformation("Merge completed. Original slots: {OriginalCount}, Merged slots: {MergedCount}",
                slots.Count, mergedSlots.Count);

            return mergedSlots;
        }


    }
}
