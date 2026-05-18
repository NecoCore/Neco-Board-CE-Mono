using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Interfaces;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.Files
{
    [ApiController]
    [Authorize]
    [Route("files/avatars")]
    public class AvatarsController : UserAuth
    {
        private readonly IFileStorage _storage;
        private readonly AccountRepository _repository;

        public AvatarsController(IFileStorage storage, AccountRepository repository)
        {
            _storage = storage;
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (UserId is null) return Unauthorized();
            await using var stream = file.OpenReadStream();
            var path = await _storage.SaveAsync(stream, file.FileName, "avatars");
            var result = await _repository.UpdateAvatar(UserId, path);
            return result.Success ? Ok(new { path }) : BadRequest(new { result.Message });
        }

        [HttpGet("{*filePath}")]
        public async Task<IActionResult> GetAvatar(string filePath)
        {
            var decodedPath = Uri.UnescapeDataString(filePath);

            var stream = await _storage.GetAsync(decodedPath);
            if (stream is null) return NotFound();

            return File(stream, "image/jpeg");
        }
    }
}
