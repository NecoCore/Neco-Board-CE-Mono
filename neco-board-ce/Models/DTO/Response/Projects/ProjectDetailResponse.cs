using neco_board_ce.Models.Entity;

namespace neco_board_ce.Models.DTO.Response.Projects
{
    public class ProjectDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
        public Guid OwnerId { get; set; }
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
            IsArchived = project.IsArchived;
            OwnerId = project.OwnerId;
            OwnerName = project.Owner?.Name ?? "Unknown";
            CreatedAt = project.CreatedAt;
            ColumnCount = project.Columns.Count;
            TaskCount = project.Columns.Sum(c => c.Tasks.Count);
            MemberCount = project.UserProjectRoles.Count;
        }
    }
}
