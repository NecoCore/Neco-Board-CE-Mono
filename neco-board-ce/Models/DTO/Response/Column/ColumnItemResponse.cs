namespace neco_board_ce.Models.DTO.Response.Column
{
    public class ColumnItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Queue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ColumnItemResponse(neco_board_ce.Models.Entity.Column column) 
        {
            Id = column.Id;
            Name = column.Name;
            Queue = column.Queue;
            CreatedAt = column.CreatedAt;
        }
    }
}
