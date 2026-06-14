namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskColumnUpdated</c> socket event (task moved between columns).
    /// </summary>
    public class TaskColumnUpdatedResponse
    {
        public Guid OldColumnId { get; set; }
        public Guid NewColumnId { get; set; }
    }
}
