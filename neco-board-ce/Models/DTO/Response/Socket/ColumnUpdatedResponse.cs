namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>ColumnUpdated</c> socket event.
    /// </summary>
    public class ColumnUpdatedResponse
    {
        public Guid ColumnId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
