using neco_board_ce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request
{
    public class EditWorkspaceRoleRequest
    {
        [Required]
        public WorkspaceRoles Role { get; set; }
    }
}
