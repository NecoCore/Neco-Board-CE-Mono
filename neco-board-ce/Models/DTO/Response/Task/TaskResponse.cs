using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Task
{
    public class TaskResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public ColumnTaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid ColumnId { get; set; }
    }
}
