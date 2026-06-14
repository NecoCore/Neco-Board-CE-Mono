using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Task
{
    public class MyTaskResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ColumnTaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime CreateAt { get; set; }
        public Guid ColumnId { get; set; }
    }
}
