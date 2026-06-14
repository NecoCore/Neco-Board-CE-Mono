using neco_board_ce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request.Projects
{
    public class EditUserInProjectRequest
    {
        [Required]
        public ProjectRole Role { get; set; }
    }
}
