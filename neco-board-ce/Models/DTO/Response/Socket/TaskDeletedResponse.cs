namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskDeleted</c> socket event.
    /// </summary>
    public class TaskDeletedResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public string ColumnId { get; set; } = string.Empty;
    }
}
