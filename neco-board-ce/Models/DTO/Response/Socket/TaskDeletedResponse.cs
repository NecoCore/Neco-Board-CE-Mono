namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskDeleted</c> socket event.
    /// </summary>
    public class TaskDeletedResponse
    {
        public Guid TaskId { get; set; }
        public Guid ColumnId { get; set; }
    }
}
