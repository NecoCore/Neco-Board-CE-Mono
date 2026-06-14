using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.Entity;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.Files
{
    /// <summary>
    /// Manages file attachments for tasks: upload, list, download, and delete.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("files/task/{taskId:guid}/attachments")]
    [Tags("Task attachments")]
    public class TaskAttachmentsController : UserAuth
    {
        private static readonly HashSet<string> AllowedExtensions =
            [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
             ".txt", ".csv", ".zip", ".rar", ".7z",
             ".jpg", ".jpeg", ".png", ".gif", ".webp",
             ".mp4", ".mov", ".avi"];

        private readonly IFileStorage _storage;
        private readonly ILogger<TaskAttachmentsController> _logger;
        private readonly TaskAttachmentsRepository _repository;
        private readonly UserAccessCheck _userAccess;
        private readonly IRealtimeNotifier _realtime;
        private readonly long _maxFileSize;
        private readonly FileExtensionContentTypeProvider _mimeProvider;

        public TaskAttachmentsController(
            IFileStorage storage,
            ILogger<TaskAttachmentsController> logger,
            TaskAttachmentsRepository repository,
            UserAccessCheck userAccess,
            IRealtimeNotifier realtime,
            IConfiguration config,
            FileExtensionContentTypeProvider mimeProvider)
        {
            _storage = storage;
            _logger = logger;
            _repository = repository;
            _userAccess = userAccess;
            _realtime = realtime;
            _maxFileSize = config.GetValue<long>("Storage:MaxFileSizeBytes", 10 * 1024 * 1024);
            _mimeProvider = mimeProvider;
        }

        /// <summary>
        /// Get Task Attachments
        /// </summary>
        /// <remarks>Returns all attachments for the specified task.</remarks>
        /// <response code="200">Returns the list of attachments.</response>
        /// <response code="204">The task has no attachments.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="500">Repository failure.</response>
        [HttpGet(Name = "GetTaskAttachments")]
        [ProducesResponseType(typeof(List<TaskAttachments>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAttachments(Guid taskId)
        {
            var access = await _userAccess.HasAccessToTask(UserId!.Value, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByTaskId(taskId);
            if (!result.Success)
            {
                _logger.LogError("Failed to get attachments for task '{TaskId}': {Error}", taskId, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve attachments." });
            }

            if (result.Data is null || result.Data.Count == 0) return NoContent();
            return Ok(result.Data);
        }

        /// <summary>
        /// Upload Task Attachment
        /// </summary>
        /// <remarks>Uploads a file and saves it as an attachment to the specified task.</remarks>
        /// <response code="201">Attachment uploaded. Returns the attachment record.</response>
        /// <response code="400">No file provided.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="500">Storage or repository failure.</response>
        [HttpPost(Name = "UploadTaskAttachment")]
        [ProducesResponseType(typeof(TaskAttachments), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadAttachment(Guid taskId, IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new ErrorMessageResponse { Message = "No file provided." });

            if (file.Length > _maxFileSize)
                return BadRequest(new ErrorMessageResponse { Message = $"File exceeds the maximum allowed size of {_maxFileSize / 1024 / 1024} MB." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new ErrorMessageResponse { Message = $"File type '{ext}' is not allowed." });

            var access = await _userAccess.HasAccessToTask(UserId!.Value, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            await using var stream = file.OpenReadStream();
            var path = await _storage.SaveAsync(stream, file.FileName, $"tasks/{taskId}/attachments");

            var entity = new TaskAttachments
            {
                TaskId = taskId,
                Name = file.FileName,
                Type = Path.GetExtension(file.FileName).TrimStart('.'),
                FilePath = path
            };

            var result = await _repository.Create(entity);
            if (!result.Success)
            {
                _logger.LogError("Failed to save attachment for task '{TaskId}': {Error}", taskId, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Failed to save attachment." });
            }

            await _realtime.TaskAttachmentUploaded(taskId);
            return CreatedAtAction(nameof(GetAttachments), new { taskId }, entity);
        }

        /// <summary>
        /// Download Task Attachment
        /// </summary>
        /// <remarks>Returns the raw file bytes for the specified attachment.</remarks>
        /// <response code="200">Returns the file.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="404">Attachment record or file not found.</response>
        [HttpGet("{attachmentId:guid}", Name = "DownloadTaskAttachment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadAttachment(Guid taskId, Guid attachmentId)
        {
            var access = await _userAccess.HasAccessToTask(UserId!.Value, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetById(attachmentId);
            if (!result.Success || result.Data is null) return NotFound();
            if (result.Data.TaskId != taskId) return NotFound();

            var stream = await _storage.GetAsync(result.Data.FilePath);
            if (stream is null) return NotFound();

            var ext = Path.GetExtension(result.Data.Name);
            var contentType = _mimeProvider.TryGetContentType(ext, out var mime) ? mime : "application/octet-stream";

            return File(stream, contentType, result.Data.Name);
        }

        /// <summary>
        /// Delete Task Attachment
        /// </summary>
        /// <remarks>Deletes the attachment record from the database and removes the file from storage.</remarks>
        /// <response code="204">Deleted successfully.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">No access to the task's project.</response>
        /// <response code="404">Attachment not found.</response>
        /// <response code="500">Repository failure.</response>
        [HttpDelete("{attachmentId:guid}", Name = "DeleteTaskAttachment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAttachment(Guid taskId, Guid attachmentId)
        {
            var access = await _userAccess.HasAccessToTask(UserId!.Value, taskId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var existing = await _repository.GetById(attachmentId);
            if (!existing.Success || existing.Data is null) return NotFound();
            if (existing.Data.TaskId != taskId) return NotFound();

            var filePath = existing.Data.FilePath;

            var result = await _repository.Delete(attachmentId);
            if (!result.Success)
            {
                _logger.LogError("Failed to delete attachment '{AttachmentId}': {Error}", attachmentId, result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Failed to delete attachment." });
            }

            await _storage.DeleteAsync(filePath);
            await _realtime.TaskAttachmentDeleted(taskId, attachmentId);
            return NoContent();
        }
    }
}
