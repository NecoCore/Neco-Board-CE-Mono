using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Task
{
    public class MyTaskResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ColumnTaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime CreateAt { get; set; }
        public string ColumnId { get; set; } = string.Empty;
    }
}
