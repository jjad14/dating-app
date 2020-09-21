using System;
using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.Dtos
{

    // properties that are used to register a user
    public class UserForRegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Gender { get; set; }
        public string KnownAs { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }


        public UserForRegisterDto()
        {
            Created = DateTime.Now;
            LastActive = DateTime.Now;
        }
    }
}