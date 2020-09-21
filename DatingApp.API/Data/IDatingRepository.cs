using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;

namespace DatingApp.API.Data
{
    public interface IDatingRepository
    {
        // Add a type of T (T in this case is user or photo) and its going to take the entity as its parameter 
        // and we can constain this method to just classes using Where 
         void Add<T>(T entity) where T: class;
         void Delete<T>(T entity) where T: class;

         // when we save our changes back to the database, there will either be 0 changes to save or more than 0
         // check to see if there have been more than one saved back to the database (True) if not then False
         // false means there were no changes to be saved back to the database or there was a problem saving back to the database
         Task<bool> SaveAll();

         Task<PagedList<User>> GetUsers(UserParams userParams);

         Task<User> GetUser(int id, bool isCurrentUser);

         Task<Photo> GetPhoto(int id);
         
         Task<Photo> GetMainPhotoForUser(int userId);

         Task<Like> GetLike(int userId, int recipientId);

         Task<Message> GetMessage(int id);

         Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams);

         // conversation between two users
         Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId);
    }
}