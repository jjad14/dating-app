using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        //inject created respository
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public AuthController(IConfiguration config, IMapper mapper,
        UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _mapper = mapper;

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // Map<DESTINATION>(SOURCE)
            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var result = await _userManager.CreateAsync(userToCreate, userForRegisterDto.Password);
            
            // Map<DESTINATION>(SOURCE)
            var userToReturn = _mapper.Map<UserForDetailedDto>(userToCreate);

            if (result.Succeeded)
            {
            return CreatedAtRoute("GetUser", 
                                  new { controller = "Users", id = userToCreate.Id }, 
                                  userToReturn);
            }

            return BadRequest(result.Errors);

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            // find user
            var user = await _userManager.FindByNameAsync(userForLoginDto.Username);

            // attempts password sign in for user
            var result = await _signInManager.CheckPasswordSignInAsync(user, userForLoginDto.Password, false);

            if (result.Succeeded)
            {
                // Map<DESTINATION>(SOURCE)
                var appUser = _mapper.Map<UserForListDto>(user);

                return Ok(new
                {
                    token = GenerateJwtToken(user).Result,
                    user = appUser
                });
            } 

            return Unauthorized();
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            //Start creating the token - if user login is success
            //token contains two claims, one is the user's Id, the other is the user's username
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            // get all role names the specified user belongs to.
            var roles = await _userManager.GetRolesAsync(user);

            // user can have multiple roles
            foreach (var role in roles)
            {
                // add each role name to the token
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            //token key
            //in appsettings.json - AppSettings:Token name - in the real world the name of token would be an extremely long randomly generated selection of characters (in the json file)
            //In order to make sure that the tokens are valid when it comes back, the server needs to sign this token 
            //creating security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            //using key from above, as part of the signing credentials and encrypting the key with HmacSha512
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            //now we create the token, we create a token descriptor and pass our claims as the subjects, add an expiry date (set to 24 hours), then pass our signing credentials from above 
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims), //pass claims as subject
                Expires = DateTime.Now.AddDays(1), //24 hour expiry
                SigningCredentials = creds  // signing credentials
            };

            //create the token handler
            var tokenHandler = new JwtSecurityTokenHandler();

            //allows us to create the token based on the token descriptor being passed in here using the token handler
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

    }
}