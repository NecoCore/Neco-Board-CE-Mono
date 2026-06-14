using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Utils.Controllers
{
    public class UserAuth : ControllerBase
    {
        protected Guid? UserId
        {
            get
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return id != null && Guid.TryParse(id, out var guid) ? guid : null;
            }
        }

        protected Guid? CurrentProjectId
        {
            get
            {
                var id = HttpContext.Items["ProjectId"]?.ToString();
                return id != null && Guid.TryParse(id, out var guid) ? guid : null;
            }
        }

        protected bool IsWorkspaceAdmin() => 
            User.FindFirstValue(ClaimTypes.Role) == WorkspaceRoles.ADMIN.ToString() || 
            User.FindFirstValue(ClaimTypes.Role) == WorkspaceRoles.OWNER.ToString();

        protected bool IsWorkspaceOwner() => 
            User.FindFirstValue(ClaimTypes.Role) == WorkspaceRoles.OWNER.ToString();
    }
}
