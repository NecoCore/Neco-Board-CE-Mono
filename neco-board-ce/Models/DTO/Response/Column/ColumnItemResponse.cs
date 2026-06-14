namespace neco_board_ce.Models.DTO.Response.Column
{
    public class ColumnItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Queue { get; set; }
        public string? Color { get; set; }
        public DateTime CreatedAt { get; set; }

        public ColumnItemResponse(neco_board_ce.Models.Entity.Column column) 
        {
            Id = column.Id;
            Name = column.Name;
            Queue = column.Queue;
            Color = column.Color;
            CreatedAt = column.CreatedAt;
        }
    }
}
