using System;
using DatingApp.API.Data;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DatingApp.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            /* 
            Since we cannot inject anything into our main method, we still need to get an instance of our DataContext
            so we can pass that into our seed users method
            and because we want to dispose of our Datacontext as soon as we've seeded our users
            therfore we use a using statement
            */
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                /* 
                try/catch to catch any errors inside here, since this is our main method when our application is starting up
                it's not something we're going to add as middleware into our HTTP request pipeline and we dont have any error
                exception handling
                */
                try 
                {
                    // get our DataContext
                    var context = services.GetRequiredService<DataContext>(); 

                    var userManager = services.GetRequiredService<UserManager<User>>();
                    var roleManager = services.GetRequiredService<RoleManager<Role>>();

                    // database migrate command
                    // applies any pending migrations for the context to the database - will create the database if it does not already exist
                    context.Database.Migrate();

                    Seed.SeedUsers(userManager, roleManager);

                    // when we start our application we are not only going to create our database but we're going to seed our users in there
                    // IF the database doesnt currently have any users in it
                }
                catch(Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occured during migration");
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
