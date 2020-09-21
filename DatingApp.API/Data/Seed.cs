using System.Collections.Generic;
using System.Linq;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace DatingApp.API.Data
{
    // takes json data (UserSeedData.json), take json objects and serialize them into user objects to match the models
    public class Seed
    {
        public static void SeedUsers(UserManager<User> userManager, RoleManager<Role> roleManager) 
        {
            // in order to seed data, make sure there are no users in database 
            if(!userManager.Users.Any())
            {
                //read from our UserSeedData.json file
                var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
                //convert to user objects that we can then loop through
                var users = JsonConvert.DeserializeObject<List<User>>(userData);

                // create some roles that we can put our users into
                var roles = new List<Role>
                {
                    new Role {Name= "Member"},
                    new Role {Name= "Admin"},
                    new Role {Name= "Moderator"},
                    new Role {Name= "VIP"},
                };

                foreach (var role in roles)
                {
                    roleManager.CreateAsync(role).Wait();
                }

                foreach (var user in users)
                {
                    // photos that we're initially seeding are all going to be automatically approved
                    user.Photos.SingleOrDefault().IsApproved = true;
                    // default passwords for seeded users is password
                    userManager.CreateAsync(user, "password").Wait();
                    // seeded users will be defaulted to the role type member
                    userManager.AddToRoleAsync(user, "Member");
                }

                // define admin user
                var adminUser = new User 
                {
                    UserName = "Admin"
                };

                // create admin user
                var result = userManager.CreateAsync(adminUser, "password").Result;

                if (result.Succeeded)
                {   // add admin to the named role
                    var admin = userManager.FindByNameAsync("Admin").Result;
                    userManager.AddToRolesAsync(admin, new[] {"Admin", "Moderator"});
                }

                
                // in order to make it run when application starts, we put it in program.cs
            }
        }
        
        // Not needed since we use Microsoft.AspNetCore.Identity;
        // create hashpassword using given password, store in passwordHash and passwordSalt
        // passwordHash and passwordSalt are given as a reference, so no return is needed
        // private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        // {
        //     //HashAlogrithm implements IDisposable, thus, HMACSHA512 is disposed after use 
        //     using(var hmac = new System.Security.Cryptography.HMACSHA512())
        //     {
        //         passwordSalt = hmac.Key; // generates random salt
        //         passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)); //compute hash using password (converted to byte array) and salt
        //     }
            
        // }
    }
}