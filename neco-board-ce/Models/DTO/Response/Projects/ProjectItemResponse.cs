using neco_board_ce.Models.Entity;

namespace neco_board_ce.Models.DTO.Response.Projects
{
    public class ProjectItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public ProjectItemResponse(Project project)
        {
            Id = project.Id;
            Name = project.Name;
        }
    }
}
