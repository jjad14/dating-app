namespace DatingApp.API.Helpers
{
    // params for getting/returning a list of messages using pagination
    public class MessageParams
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;
        private int pageSize = 10;
        public int PageSize
        {
            get { return pageSize; }
            set { pageSize = (value>MaxPageSize)? MaxPageSize: value ; }
        }
        
        public int UserId { get; set; }
        public string MessageContainer { get; set; } = "Unread";
    }
}