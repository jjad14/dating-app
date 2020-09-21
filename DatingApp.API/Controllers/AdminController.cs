using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    // localhost:5000/api/admin
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private Cloudinary _cloudinary;
        
        public AdminController(
            DataContext context,
            UserManager<User> userManager,
            IOptions<CloudinarySettings> cloudinaryConfig) {

            _context = context;
            _userManager = userManager;
            _cloudinaryConfig = cloudinaryConfig;

            // Define Cloudinary account
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            // create cloudinary account
            _cloudinary = new Cloudinary(acc);
        }

    // only admins can access this endpoint
    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("usersWithRoles")]
    public async Task<IActionResult> GetUserWithRoles()
    {
        // Get userlist, alternative to mapping data, 
        // we are just selecting what we want to return for each user   
        var userList = await _context.Users
            .OrderBy(x => x.UserName)
            .Select(user => new
            {
                Id = user.Id,
                UserName = user.UserName,
                Roles = (from userRole in user.UserRoles
                         join role in _context.Roles
                         on userRole.RoleId
                         equals role.Id
                         select role.Name).ToList()
            }).ToListAsync();

        return Ok(userList);
    }

    // only admins can access this endpoint
    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("editRoles/{userName}")]
    public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
    {
        // get user by username
        var user = await _userManager.FindByNameAsync(userName);

        // get roles of that specific user
        var userRoles = await _userManager.GetRolesAsync(user);

        // which roles have been selected
        var selectedRoles = roleEditDto.RoleNames;

        // ?? is referred to as the null coalescing operator
        //  shortform of: selected = selectedRoles != null ? selectedRoles : new string[] {};
        selectedRoles = selectedRoles ?? new string[] { };

        // add user to specific role except those that the user is already assigned to
        var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to add to roles");

        // remove roles that were not selected
        result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to remove the roles");


        // return roles of the user
        return Ok(await _userManager.GetRolesAsync(user));
    }

    // admins and moderators can access this endpoint
    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photosForModeration")]
    public async Task<IActionResult> GetPhotosForModeration()
    {
        // IgnoreQueryFilters so we get all photes (approved and awaiting)
        // get all photos that are awaiting approval/rejection (id/user/url/isapproved)
        var photos = await _context.Photos
            .Include(u => u.User)
            .IgnoreQueryFilters()
            .Where(p => p.IsApproved == false)
            .Select(u => new 
            {
                Id = u.Id,
                UserName = u.User.UserName,
                Url = u.Url,
                IsApproved = u.IsApproved
            }).ToListAsync();


        return Ok(photos);
    }

    // admins and moderators can access this endpoint
    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approvePhoto/{photoId}")]
    public async Task<IActionResult> ApprovePhoto(int photoId)
    {
        // IgnoreQueryFilters so we get all photes (approved and awaiting)
        // get photo by the id
        var photo = await _context.Photos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == photoId);

        // approve photo
        photo.IsApproved = true;

        // save changes to db
        await _context.SaveChangesAsync();

        return Ok();
    }

    // admins and moderators can access this endpoint
    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("rejectPhoto/{photoId}")]
    public async Task<IActionResult> RejectPhoto(int photoId)
    {
        // IgnoreQueryFilters so we get all photes (approved and awaiting)
        // get photo by the id
        var photo = await _context.Photos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == photoId);

        // if its a cloudinary photo (has publicId)
        if (photo.PublicId != null)
        {
            // Parameters for deletion of a single asset from your Cloudinary account.
            var deleteParams = new DeletionParams(photo.PublicId);
            var result = _cloudinary.Destroy(deleteParams);

            if (result.Result == "ok")
            {
                _context.Photos.Remove(photo);
            }
        }

        // not a cloudinary photo
        if (photo.PublicId == null)
        {
            _context.Photos.Remove(photo);
        }

        // save changes
        await _context.SaveChangesAsync();

        return Ok();
    }

    }
}