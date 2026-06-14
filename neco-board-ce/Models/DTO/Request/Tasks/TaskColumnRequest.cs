using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Request.Tasks
{
    public class TaskColumnRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid ColumnId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; } = TaskPriority.LOW;
        public ColumnTaskStatus Status { get; set; } = ColumnTaskStatus.NOT_STARTED;
    }
}
