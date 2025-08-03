namespace MeetinRoomRezervation.Extensions
{
    public static class TimeZoneExtension
    {
        public static DateTime ConvertToTimeZone(this DateTime dateTime, string timeZoneId)
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTime(dateTime, timeZone);
        }
    }
}
