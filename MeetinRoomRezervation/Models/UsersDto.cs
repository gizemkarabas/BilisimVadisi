﻿namespace MeetinRoomRezervation.Models
{
    public class UserDto
    {

        public string Id { get; set; }

        public required string Email { get; set; }

        public required string PasswordHash { get; set; }
    }

}
