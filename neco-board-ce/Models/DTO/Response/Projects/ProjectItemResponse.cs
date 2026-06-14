using neco_board_ce.Models.Entity;

namespace neco_board_ce.Models.DTO.Response.Projects
{
    public class ProjectItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsArchived { get; set; }

        public ProjectItemResponse(Project project)
        {
            Id = project.Id;
            Name = project.Name;
            IsArchived = project.IsArchived;
        }
    }
}
