namespace DatingApp.API.Models
{
    public class Like
    {
        // id of user that likes another user
        public int LikerId { get; set; }

        // id of user being liked by another user
        public int LikeeId { get; set; }
        public virtual User Liker { get; set; }
        public virtual User Likee { get; set; }
    }
}