using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Task
{
    public class TaskDetailResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Text { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public ColumnTaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ColumnId { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        
        public List<string> AssignedUserNames { get; set; } = [];
        public int ImageCount { get; set; }
        public int AttachmentCount { get; set; }

        public TaskDetailResponse() { }

        public TaskDetailResponse(ColumnTask task)
        {
            Id = task.Id;
            Name = task.Name;
            Description = task.Description;
            Text = task.Text;
            Priority = task.Priority;
            Status = task.Status;
            CreatedAt = task.CreatedAt;
            ColumnId = task.ColumnId;
            OwnerId = task.OwnerId;
            OwnerName = task.Owner?.Name ?? "Unknown";
            
            AssignedUserNames = task.Users.Select(u => u.User?.Name ?? "Unknown").ToList();
            ImageCount = task.Images.Count;
            AttachmentCount = task.Attachments.Count;
        }
    }
}
