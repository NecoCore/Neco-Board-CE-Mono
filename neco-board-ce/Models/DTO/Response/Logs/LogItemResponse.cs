using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Logs
{
    public class LogItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LogType LogType { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        
        public string? ProjectId { get; set; }
        public string? ProjectName { get; set; }

        public LogItemResponse() { }

        public LogItemResponse(Logs log)
        {
            Id = log.Id;
            Name = log.Name;
            Description = log.Description;
            LogType = log.LogType;
            CreatedAt = log.CreatedAt;
            UserId = log.UserId;
            UserName = log.User?.Name ?? "Unknown";
            ProjectId = log.ProjectId;
            ProjectName = log.Project?.Name;
        }
    }
}
