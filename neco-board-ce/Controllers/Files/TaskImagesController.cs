using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.Entity;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.Files
{
    /// <summary>
    /// Manages images for tasks: upload, list, get, and delete.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("files/task/{taskId}/images")]
    [Tags("Task images")]
    public class TaskImagesController : UserAuth
    {
        private readonly IFileStorage _storage;
        private readonly ILogger<TaskImagesController> _logger;
        private readonly TaskImagesRepository _repository;
        private readonly UserAccessCheck _userAccess;
        private readonly IRealtimeNotifier _realtime;

        public TaskImagesController(
            IFileStorage storage,
            ILogger<TaskImagesController> logger,
            TaskImagesRepository repository,
            UserAccessCheck userAccess,
            IRealtimeNotifier realtime)
        {
            _storage = storage;
            _logger = logger;
            _repository = repository;
            _userAccess = userAccess;
            _realtime = realtime;
        }

        /// <summary>
        /// Get Task Images
        /// </summary>
        /// <remarks>Returns the list of all image records attached to the specified task.</remarks>
        /// <response code="200">Returns the list of image records.</response>
        /// <response code="204">The task has no images.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="500">Repository failure.</response>
        [HttpGet(Name = "GetTaskImages")]
        [ProducesResponseType(typeof(List<TaskImages>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetImages(string taskId)
        {
            var access = await _userAccess.HasAccessToTask(UserId!, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByTaskId(taskId);
            if (!result.Success)
            {
                _logger.LogError("Failed to get images for task '{TaskId}': {Error}", taskId, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve images." });
            }

            if (result.Data is null || result.Data.Count == 0) return NoContent();
            return Ok(result.Data);
        }

        /// <summary>
        /// Upload Task Image
        /// </summary>
        /// <remarks>
        /// Uploads an image file and attaches it to the specified task.
        /// Only image content types are accepted (image/*).
        /// </remarks>
        /// <response code="201">Image uploaded. Returns the image record.</response>
        /// <response code="400">No file provided or file is not an image.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="500">Storage or repository failure.</response>
        [HttpPost(Name = "UploadTaskImage")]
        [ProducesResponseType(typeof(TaskImages), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadImage(string taskId, IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new ErrorMessageResponse { Message = "No file provided." });

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new ErrorMessageResponse { Message = "Only image files are allowed." });

            var access = await _userAccess.HasAccessToTask(UserId!, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            await using var stream = file.OpenReadStream();
            var path = await _storage.SaveAsync(stream, file.FileName, $"tasks/{taskId}/images");

            var entity = new TaskImages
            {
                TaskId = taskId,
                Name = file.FileName,
                ImagePath = path
            };

            var result = await _repository.Create(entity);
            if (!result.Success)
            {
                _logger.LogError("Failed to save image for task '{TaskId}': {Error}", taskId, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Failed to save image." });
            }

            await _realtime.TaskImageUploaded(taskId);
            return CreatedAtAction(nameof(GetImages), new { taskId }, entity);
        }

        /// <summary>
        /// Get Task Image
        /// </summary>
        /// <remarks>Returns the raw image bytes for the specified image record.</remarks>
        /// <response code="200">Returns the image file.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="404">Image record or file not found.</response>
        [HttpGet("{imageId}", Name = "GetTaskImage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImage(string taskId, string imageId)
        {
            var access = await _userAccess.HasAccessToTask(UserId!, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetById(imageId);
            if (!result.Success || result.Data is null) return NotFound();
            if (result.Data.TaskId != taskId) return NotFound();

            var stream = await _storage.GetAsync(result.Data.ImagePath);
            if (stream is null) return NotFound();

            return File(stream, "image/jpeg");
        }

        /// <summary>
        /// Delete Task Image
        /// </summary>
        /// <remarks>Deletes the image record from the database and removes the file from storage.</remarks>
        /// <response code="204">Deleted successfully.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="404">Image not found.</response>
        /// <response code="500">Repository failure.</response>
        [HttpDelete("{imageId}", Name = "DeleteTaskImage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteImage(string taskId, string imageId)
        {
            var access = await _userAccess.HasAccessToTask(UserId!, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var existing = await _repository.GetById(imageId);
            if (!existing.Success || existing.Data is null) return NotFound();
            if (existing.Data.TaskId != taskId) return NotFound();

            var imagePath = existing.Data.ImagePath;

            var result = await _repository.Delete(imageId);
            if (!result.Success)
            {
                _logger.LogError("Failed to delete image '{ImageId}': {Error}", imageId, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Failed to delete image." });
            }

            await _storage.DeleteAsync(imagePath);
            await _realtime.TaskImageDeleted(taskId, imageId);
            return NoContent();
        }
    }
}
