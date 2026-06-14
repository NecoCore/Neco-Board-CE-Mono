using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Request.Projects
{
    public class UserProjectRequest
    {
        public string Id { get; set; }
        public ProjectRole Role { get; set; } = ProjectRole.USER;
    }
}
