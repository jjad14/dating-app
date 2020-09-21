using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CloudinaryDotNet;
using System.Threading.Tasks;
using DatingApp.API.Dtos;
using System.Security.Claims;
using CloudinaryDotNet.Actions;
using DatingApp.API.Models;
using System.Linq;

namespace DatingApp.API.Controllers
{
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper,
        IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper; 
            _repo = repo;

            // setup cloudinary account
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);

        }

        //api/users/{userId}/photos/{id}
        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id) 
        {
            // get photo by id
            var photoFromRepo = await _repo.GetPhoto(id);

            // map photoFromRepo to PhotoForReturnDto
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm] PhotoForCreationDto photoForCreationDto) 
        {
            // we need to compare the id of the path to the users id thats being passed in as part of their token
            // if the user id doesnt match the id in the token then an unauthrorized is given
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            // get user from repos
            var userFromRepo = await _repo.GetUser(userId, true);

            // one of the properties inside photoForCreationDto should be the file itself
            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if(file.Length > 0)
            {
                // reads file into memory, 
                // since this is going to be a file stream, we'll use 'using' so that we can dispose of the file in memory once we completed this method
                using (var stream = file.OpenReadStream()) 
                {
                    // give cloudinary our upload param
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        // we want to transform the image so that if we upload an incredibly long photo of a user for instance,
                        // its going to crop the image automatically for us and focus in on the face and crop the area around the face and store a square image
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    // response from cloudinary upload
                    uploadResult = _cloudinary.Upload(uploadParams); 
                }
            }

            // start populating other fields inside photoForCreationDto
            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            // map photoForCreationDto into the photo model
            var photo = _mapper.Map<Photo>(photoForCreationDto);

            // when a user uploads a photo this might be an additional photo to their other photos, or first photo and if so then we want to set it to be their main photo

            // user doesnt have a main photo
            if(!userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = true;

            userFromRepo.Photos.Add(photo);

            if(await _repo.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);

                // .Net Core 3.0 - we need to provide all route parameters
                return CreatedAtRoute("GetPhoto", new { userId = userId, id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");   

        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            // we need to compare the id of the path to the users id thats being passed in as part of their token
            // if the user id doesnt match the id in the token then an unauthrorized is given
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();
            
            var user = await _repo.GetUser(userId, true);

            // if the id of the photo does not match any of the photos id in the user photo collection then return unauthorized 
            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await _repo.GetPhoto(id);

            // check if photo is the main photo
            if (photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");

            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);

            // set current main photo to false since new photo will be main
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true; 

            if (await _repo.SaveAll())
                return NoContent();

            return BadRequest("Could not set photo to main");
        } 

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            // we need to compare the id of the path to the users id thats being passed in as part of their token
            // if the user id doesnt match the id in the token then an unauthrorized is given
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();
            
            var user = await _repo.GetUser(userId, true);

            // if the id of the photo does not match any of the photos id in the user photo collection then return unauthorized 
            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await _repo.GetPhoto(id);

            // check if photo is the main photo
            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");

            // Cloudinary photos have publicID, so if its a cloudinary photo then its public id cannot be null
            if (photoFromRepo.PublicId != null)
            {
                // deletion params contains public id of photo
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);    

                // remove photo from cloudinary
                var result = _cloudinary.Destroy(deleteParams);

                // if deletion is successful, result should say ok
                if (result.Result == "ok") {
                    // remove from database
                    _repo.Delete(photoFromRepo);
                }
            } 

            // for photos that arent from cloudinary
            if (photoFromRepo.PublicId == null) 
            {
                // remove from database
                _repo.Delete(photoFromRepo);
            }

            // save changes to repo
            if (await _repo.SaveAll()) 
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}