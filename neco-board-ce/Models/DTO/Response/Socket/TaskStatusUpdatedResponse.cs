using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskStatusUpdated</c> socket event.
    /// </summary>
    public class TaskStatusUpdatedResponse
    {
        public Guid TaskId { get; set; }
        public Guid ColumnId { get; set; }
        public ColumnTaskStatus Status { get; set; }
    }
}
