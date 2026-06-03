namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>TaskColumnUpdated</c> socket event (task moved between columns).
    /// </summary>
    public class TaskColumnUpdatedResponse
    {
        public string OldColumnId { get; set; } = string.Empty;
        public string NewColumnId { get; set; } = string.Empty;
    }
}
