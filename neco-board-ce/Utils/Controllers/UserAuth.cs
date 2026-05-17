using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Utils.Controllers
{
    public class UserAuth : ControllerBase
    {
        protected string? UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected bool IsWorkspaceAdmin() => 
            User.FindFirstValue(ClaimTypes.Role) == WorkspaceRoles.ADMIN.ToString() || 
            User.FindFirstValue(ClaimTypes.Role) == WorkspaceRoles.OWNER.ToString();

        protected bool IsWorkspaceOwner() => 
            User.FindFirstValue(ClaimTypes.Role) == WorkspaceRoles.OWNER.ToString();
    }
}
