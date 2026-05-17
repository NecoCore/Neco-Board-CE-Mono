using neco_board_ce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request
{
    public class EditUserInProjectRequest
    {
        [Required]
        public ProjectRole Role { get; set; }
    }
}
