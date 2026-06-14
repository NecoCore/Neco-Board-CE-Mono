using neco_board_ce.Models.Entity;

namespace neco_board_ce.Models.DTO.Response.Projects
{
    public class ProjectDetailResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ColumnCount { get; set; }
        public int TaskCount { get; set; }
        public int MemberCount { get; set; }

        public ProjectDetailResponse() { }

        public ProjectDetailResponse(Project project)
        {
            Id = project.Id;
            Name = project.Name;
            Description = project.Description;
            OwnerId = project.OwnerId;
            OwnerName = project.Owner?.Name ?? "Unknown";
            CreatedAt = project.CreatedAt;
            ColumnCount = project.Columns.Count;
            TaskCount = project.Columns.Sum(c => c.Tasks.Count);
            MemberCount = project.UserProjectRoles.Count;
        }
    }
}
