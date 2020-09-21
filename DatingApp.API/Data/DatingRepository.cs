using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DatingApp.API.Helpers;
using System;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;

        public DatingRepository(DataContext context)
        {
            _context = context;

        }

        // no async method just synchronous code because when we add something into our context, we're not querying or doing anyhting with the database
        public void Add<T>(T entity) where T : class
        {
            // saved in memory until we actually save our changes back to the datbase
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.Where(u => u.UserId == userId).FirstOrDefaultAsync(p => p.IsMain);
        }

        // we pass the id, then the id is used to extract the first or default from the database that matches the id that we're passing in
        // if we dont have a matching id in our database then we return the default for this particular type which is gonna be a user object
        // default is null
        public async Task<User> GetUser(int id, bool isCurrentUser)
        {
            var query = _context.Users.Include(p => p.Photos).AsQueryable();

            // if user is the current user, then return all photes approved or not
            if (isCurrentUser)
                query = query.IgnoreQueryFilters();

            // variable to store user and photos -  we want to show a user with their main profile photo
            // photos are considered navigation properties, so we must tell Entity Framework to include it
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }

        // return a PagedList of users with their photos
        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.OrderByDescending(u => u.LastActive).AsQueryable();
            //var users = _context.Users.Include(p=>p.Photos);

            // get all users except current user
            users = users.Where(u => u.Id != userParams.UserId);
            // get all users of the opposite gender
            users = users.Where(u => u.Gender == userParams.Gender);

            if(userParams.Likers)
            {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            if (userParams.Likees)
            {
                var userLikess = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikess.Contains(u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) 
            {
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy)) 
            {
                switch (userParams.OrderBy) 
                {
                    case "created": 
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            // get the logged in user that includes the likes and likees collection
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            // return all the users that have liked the currently logged in user
            if(likers) 
            {
                // we dont want to return a collection of users but instead a collection of ints, so we use select 
                return user.Likers.Where(u => u.LikeeId == id).Select(i => i.LikerId);
            }
            else // return all the users that the currently logged in user has liked
            {
                return user.Likees.Where(u => u.LikerId == id).Select(i => i.LikeeId);
            }
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            
            var messages = _context.Messages.AsQueryable();

            switch(messageParams.MessageContainer) 
            {
                case "Inbox":
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.RecipientDeleted == false);
                    break;
                case "Outbox":
                    messages = messages.Where(u => u.SenderId == messageParams.UserId && u.SenderDeleted == false);
                    break;
                default:
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.RecipientDeleted == false && u.IsRead == false);
                    break;
            }

            messages = messages.OrderByDescending(d => d.MessageSent);

            return await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await _context.Messages
                .Where(m => m.RecipientId == userId 
                    && m.RecipientDeleted == false && m.SenderId == recipientId 
                    || m.RecipientId == recipientId 
                    && m.SenderId == userId && m.SenderDeleted == false)
                .OrderByDescending(m => m.MessageSent)
                .ToListAsync();

            return messages;
        }
    }
}