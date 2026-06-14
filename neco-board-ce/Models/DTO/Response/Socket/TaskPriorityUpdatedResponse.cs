using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskPriorityUpdated</c> socket event.
    /// </summary>
    public class TaskPriorityUpdatedResponse
    {
        public Guid TaskId { get; set; }
        public Guid ColumnId { get; set; }
        public TaskPriority Priority { get; set; }
    }
}
