using MeetinRoomReservation.Constants;
using MeetinRoomRezervation.Constants;
using MeetinRoomRezervation.Data;
using MeetinRoomRezervation.Extensions;
using MeetinRoomRezervation.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MeetinRoomRezervation.Services.ReservationService
{
    public class RoomService : IRoomService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<RoomService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RoomService(
            MongoDbContext context,
            ILogger<RoomService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public async Task<MeetingRoomDto> GetRoomByIdAsync(string roomId)
        {
            try
            {
                //var objectId = new ObjectId(roomId);
                var filter = Builders<Data.MeetingRoom>.Filter.Eq("_id", roomId);
                var room = await _context.Rooms.Find(filter).FirstOrDefaultAsync();

                if (room == null)
                {
                    return null;
                }

                return new MeetingRoomDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    Location = room.Location,
                    Capacity = room.Capacity,
                    IsAvailable = true,
                    OccupancyRate = 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRoomByIdAsync: {ex.Message}");
                throw;
            }
        }
        public async Task<List<MeetingRoomDto>> GetAllRoomsAsync()
        {
            var rooms = await _context.Rooms.Find(_ => true).ToListAsync();

            return rooms.Select(room => new MeetingRoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Location = room.Location,
                Capacity = room.Capacity,
                IsAvailable = true,
                OccupancyRate = 0
            }).ToList();
        }
        public async Task UpdateRoomAsync(MeetingRoomDto roomDto)
        {
            var filter = Builders<MeetingRoom>.Filter.Eq(r => r.Id, roomDto.Id);

            var update = Builders<MeetingRoom>.Update
                .Set(r => r.Name, roomDto.Name)
                .Set(r => r.Capacity, roomDto.Capacity);

            if (!string.IsNullOrEmpty(roomDto.Location))
            {
                update = update.Set(r => r.Location, roomDto.Location);
            }

            await _context.Rooms.UpdateOneAsync(filter, update);
        }
        public async Task DeleteRoomAsync(string roomId)
        {
            var filter = Builders<MeetingRoom>.Filter.Eq(r => r.Id, roomId);
            await _context.Rooms.DeleteOneAsync(filter);
        }
        public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(string roomId, DateTime date)
        {
            var slotDuration = TimeSpan.FromHours(1); // 1 saatlik slotlar

            var reservations = await _context.Reservations
                .Find(r => r.RoomId == roomId && r.StartTime.Date == date.Date)
                .ToListAsync();

            var reservedHours = reservations
                .Select(r => r.StartTime.TimeOfDay)
                .ToHashSet();

            var slots = new List<TimeSpan>();

            for (int hour = 0; hour < 24; hour++)
            {
                var slot = TimeSpan.FromHours(hour);
                if (!reservedHours.Contains(slot))
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }
        public async Task<bool> AddRoomAsync(MeetingRoomDto roomDto)
        {
            try
            {
                var room = new Data.MeetingRoom
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = roomDto.Name,
                    Location = roomDto.Location,
                    Capacity = roomDto.Capacity,
                    OccupancyRate = 0,
                    IsAvailable = true
                };

                await _context.Rooms.InsertOneAsync(room);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddRoomAsync: {ex.Message}");
                return false;
            }
        }
        public async Task<List<SlotDto>> GetSlotsWithStatusAsync(string roomId, DateTime date)
        {
            try
            {
                var slots = new List<SlotDto>();
                var now = DateTime.UtcNow;
                bool isToday = date.Date == DateTime.Today;

                for (int hour = 0; hour < 24; hour++)
                {
                    var startTime = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0, DateTimeKind.Local);
                    var endTime = startTime.AddHours(1);

                    var slot = new SlotDto
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        IsReserved = false,
                        IsDisabled = false
                    };

                    // Bugün için geçmiş saatleri devre dışı bırak ama rezerve olarak işaretleme
                    if (isToday && startTime <= now.ConvertToTimeZone(DateConstants.DefaultTimeZone))
                    {
                        slot.IsDisabled = true;
                        slot.IsReserved = false; // Geçmiş saatler rezerve değil, sadece devre dışı
                    }

                    Console.WriteLine($"Created slot: {slot.StartTime:yyyy-MM-dd HH:mm} - {slot.EndTime:yyyy-MM-dd HH:mm}");
                    slots.Add(slot);
                }

                using var scope = _serviceProvider.CreateScope();
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var reservations = await reservationService.GetReservationsByDateAsync(date);
                var roomReservations = reservations.Where(r => r.RoomId == roomId).ToList();

                foreach (var slot in slots)
                {
                    // Sadece aktif slotlar için rezervasyon kontrolü yap
                    if (!slot.IsDisabled)
                    {
                        var isReserved = roomReservations.Any(r =>
                            r.StartTime.ToLocalTime() < slot.EndTime && r.EndTime.ToLocalTime() > slot.StartTime);

                        if (isReserved)
                        {
                            slot.IsReserved = true;
                            slot.IsDisabled = true;
                        }
                    }
                }

                return slots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots for room {RoomId} on date {Date}", roomId, date);
                return new List<SlotDto>();
            }
        }
        public async Task<List<MeetingRoomDto>> GetAllRoomsWithOccupancyAsync(DateTime date)
        {
            try
            {
                var rooms = await _context.Rooms.Find(_ => true).ToListAsync();
                var result = new List<MeetingRoomDto>();

                bool isToday = date.Date == DateTime.Today;
                double passedHour = 0;

                FilterDefinition<Reservation> reservationFilter = Builders<Reservation>.Filter.Empty;
                if (isToday)
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
                    DateTime startOfNextDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
                    reservationFilter = Builders<Reservation>.Filter.And(
                        Builders<Reservation>.Filter.Gte(r => r.StartTime, nextHour),
                        Builders<Reservation>.Filter.Lte(r => r.StartTime, startOfNextDay),
                        Builders<Reservation>.Filter.Eq(r => r.Status, ReservationStatus.Active));
                    DateTime timeZoneDate = DateTime.UtcNow.ConvertToTimeZone(DateConstants.DefaultTimeZone);
                    passedHour = Math.Floor((timeZoneDate.AddHours(1) - timeZoneDate.Date).TotalHours);
                }
                else
                {
                    DateTime startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
                    DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);
                    reservationFilter = Builders<Reservation>.Filter.And(
                        Builders<Reservation>.Filter.Gte(r => r.StartTime, startOfDay),
                        Builders<Reservation>.Filter.Lte(r => r.StartTime, endOfDay),
                        Builders<Reservation>.Filter.Eq(r => r.Status, ReservationStatus.Active));
                }

                var resevations = await _context.Reservations.Find(reservationFilter).ToListAsync();
                foreach (var room in rooms)
                {
                    int reservedHoursCount = 0;
                    var roomReservations = resevations.Where(p => p.RoomId == room.Id);

                    var totalReservedHours = roomReservations.Sum(r =>
                        Math.Floor((r.EndTime.AddMinutes(1) - r.StartTime).TotalHours));
                    totalReservedHours += passedHour;
                    double reservedHours = reservedHoursCount;
                    double occupancyRate = Math.Min(100, Math.Round((totalReservedHours / 24) * 100, 2));

                    result.Add(new MeetingRoomDto
                    {
                        Id = room.Id!,
                        Name = room.Name,
                        Capacity = room.Capacity,
                        Location = room.Location ?? StringConstants.LocationName,
                        OccupancyRate = occupancyRate,
                        IsAvailable = occupancyRate < 100
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllRoomsWithOccupancyAsync: {ex.Message}");
                throw;
            }
        }


    }

}
