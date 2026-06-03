using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskStatusUpdated</c> socket event.
    /// </summary>
    public class TaskStatusUpdatedResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public string ColumnId { get; set; } = string.Empty;
        public ColumnTaskStatus Status { get; set; }
    }
}
