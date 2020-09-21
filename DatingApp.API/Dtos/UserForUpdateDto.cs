namespace DatingApp.API.Dtos
{
    // properties that a user can update
    public class UserForUpdateDto
    {
        public string Introduction { get; set; }
        public string LookingFor { get; set; }
        public string Interest { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}