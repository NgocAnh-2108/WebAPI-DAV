using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI_DAV.Models.Domain;
using WebAPI_DAV.Models.DTO;
using WebAPI_DAV.Repositories;

namespace WebAPI_DAV.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IImageRepository _imageRepository;

        public ImagesController(IImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }

        [HttpPost]
        [Route("Upload")]
        [Authorize(Roles = "Manager")]
        public IActionResult Upload([FromForm] ImageUploadRequestDTO request)
        {
            ValidateFileUpload(request);
            if (ModelState.IsValid)
            {
                var imageDomainModel = new Image
                {
                    File = request.File,
                    FileExtension = Path.GetExtension(request.File.FileName),
                    FileSizeInBytes = request.File.Length,
                    FileName = request.FileName,
                    FileDescription = request.FileDescription,
                    RoomId = request.RoomId
                };
                _imageRepository.Upload(imageDomainModel);
                return Ok(imageDomainModel);
            }
            return BadRequest(ModelState);
        }

        [HttpGet]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult GetAllInfoImages()
        {
            var allImages = _imageRepository.GetAllInfoImages();
            return Ok(allImages);
        }

        [HttpGet]
        [Route("by-room/{roomId}")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult GetByRoom(int roomId)
        {
            var images = _imageRepository.GetImagesByRoomId(roomId);
            return Ok(images);
        }

        [HttpGet]
        [Route("Download")]
        [Authorize(Roles = "Receptionist,Manager")]
        public IActionResult DownloadImage(int id)
        {
            var result = _imageRepository.DownloadFile(id);
            return File(result.Item1, result.Item2, result.Item3);
        }

        #region Private methods
        private void ValidateFileUpload(ImageUploadRequestDTO request)
        {
            var allowExtensions = new string[] { ".jpg", ".jpeg", ".png" };
            if (!allowExtensions.Contains(Path.GetExtension(request.File.FileName)))
            {
                ModelState.AddModelError("file", "Unsupported file extension");
            }

            if (request.File.Length > 10485760)
            {
                ModelState.AddModelError("file", "File size too big, please upload file <10M");
            }
        }
        #endregion
    }
}