using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.Files
{
    /// <summary>
    /// Manages user avatars: upload and retrieve.
    /// </summary>
    /// <remarks>
    /// Upload replaces the current avatar path stored in the account record.
    /// Retrieval is public within the authenticated workspace — any logged-in user
    /// can fetch any avatar by its storage path.
    /// </remarks>
    [ApiController]
    [Authorize]
    [Route("files/avatars")]
    [Tags("Avatars")]
    public class AvatarsController : UserAuth
    {
        private readonly IFileStorage _storage;
        private readonly AccountRepository _repository;

        public AvatarsController(IFileStorage storage, AccountRepository repository)
        {
            _storage = storage;
            _repository = repository;
        }

        /// <summary>
        /// Upload Avatar
        /// </summary>
        /// <remarks>
        /// Saves the uploaded image to storage and updates the authenticated user's
        /// avatar path in the database. The previous file is not removed from storage.
        /// Returns the new storage path on success.
        /// </remarks>
        /// <param name="file">Image file to upload.</param>
        /// <response code="200">Avatar uploaded successfully. Response body contains the new storage path.</response>
        /// <response code="400">Repository failed to update the avatar path. Response body contains the error description.</response>
        /// <response code="401">The request is not authenticated.</response>
        [HttpPost(Name = "UploadAvatar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (UserId is null) return Unauthorized();
            await using var stream = file.OpenReadStream();
            var path = await _storage.SaveAsync(stream, file.FileName, "avatars");
            var result = await _repository.UpdateAvatar(UserId, path);
            return result.Success ? Ok(new { path }) : BadRequest(new { result.Message });
        }

        /// <summary>
        /// Get Avatar
        /// </summary>
        /// <remarks>
        /// Returns the raw image bytes for the given storage path.
        /// The path is URL-decoded before being passed to the storage backend,
        /// so percent-encoded separators are handled transparently.
        /// Always returns <c>image/jpeg</c> as the content type regardless of the original format.
        /// </remarks>
        /// <param name="filePath">The storage path of the avatar (supports catch-all with slashes).</param>
        /// <response code="200">Returns the avatar image.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="404">No file found at the given storage path.</response>
        [HttpGet("{*filePath}", Name = "GetAvatar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvatar(string filePath)
        {
            var decodedPath = Uri.UnescapeDataString(filePath);

            var stream = await _storage.GetAsync(decodedPath);
            if (stream is null) return NotFound();

            return File(stream, "image/jpeg");
        }
    }
}
