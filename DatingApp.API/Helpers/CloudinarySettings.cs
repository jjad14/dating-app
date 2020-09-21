namespace DatingApp.API.Helpers
{
    // props for cloudinary account settings
    // in order to tie this to the app settings in our appsettings.json file, we go to startup.cs
    public class CloudinarySettings
    {
        public string CloudName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }
}