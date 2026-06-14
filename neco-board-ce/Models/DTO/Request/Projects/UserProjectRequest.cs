using neco_board_ce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request.Projects
{
    public class UserProjectRequest
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public ProjectRole Role { get; set; } = ProjectRole.USER;
    }
}
