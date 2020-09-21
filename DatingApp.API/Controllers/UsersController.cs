using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;

        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> getUsers([FromQuery]UserParams userParams)
        {
            // get userid from token
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // get user from db
            var userFromRepo = await _repo.GetUser(currentUserId, true);

            userParams.UserId = currentUserId;

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }

            // get users by specified user params
            var users = await _repo.GetUsers(userParams);

            // map list of messages.  In order to map a list of MessageToReturnDto's we need to ensure the type we are returning is a List 
            // In this case we are using IEnumerable as this will make sure that we are returning a list rather than a single Message
            var userToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);

            // add pagination to response body
            Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(userToReturn);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> getUser(int id)
        {
            // if id matches token id then its the current user else its not
            var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;

            var user = await _repo.GetUser(id, isCurrentUser);

            // map user to UserForDetailedDto
            var userToReturn = _mapper.Map<UserForDetailedDto>(user);

            return Ok(userToReturn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto) 
        {
            // we need to compare the id of the path to the users id thats being passed in as part of their token
            // if the user id doesnt match the id in the token then an unauthrorized is given
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            // get user from db
            var userFromRepo = await _repo.GetUser(id, true);

            // then we need to take the information thats in our userForUpdateDto and map it into userFromRepo
            // this is going to execute the mapping and effectively updates the values from userForUpdateDto
            _mapper.Map(userForUpdateDto, userFromRepo);

            // save our changes, and if this is successful then what we'll return from a update method is a no content
            if (await _repo.SaveAll())
                return NoContent();

            throw new Exception($"Updating user {id} failed on save");
        }

        // recipient Id is the id the user likes
        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            // we need to compare the id of the path to the users id thats being passed in as part of their token
            // if the user id doesnt match the id in the token then an unauthrorized is given
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            // check existance of a like between the userid and recipientId
            var like = await _repo.GetLike(id, recipientId);

            // if like is not null, this means theres already a like in place for this recipient
            if (like != null) 
                return BadRequest("You already liked this user");

            // check if recipient exists
            if (await _repo.GetUser(recipientId, false) == null) 
                return NotFound();

            // if there is no existing relationship between user and recipient and recipient exists
            // then create a like with both parties id
            like = new Like 
            {
                LikerId = id,
                LikeeId = recipientId
            };

            // synchronous call, stored in memory (our repo), not in database yet
            _repo.Add<Like>(like);

            // asynchronous, saving to database
            if (await _repo.SaveAll())
                return Ok();

            return BadRequest("Failed to like user");



        }

    }
}
