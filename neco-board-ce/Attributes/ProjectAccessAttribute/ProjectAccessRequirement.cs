using Microsoft.AspNetCore.Authorization;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Attributes.ProjectAccessAttribute
{
    public class ProjectAccessRequirement : IAuthorizationRequirement
    {
        public ProjectRole? Role { get; }
        public ProjectAccessRequirement(ProjectRole? role = null) => Role = role;
    }
}
