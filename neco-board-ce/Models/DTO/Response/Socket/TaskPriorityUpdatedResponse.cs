using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskPriorityUpdated</c> socket event.
    /// </summary>
    public class TaskPriorityUpdatedResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public string ColumnId { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
    }
}
