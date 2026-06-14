using Microsoft.AspNetCore.Authorization;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Attributes.ProjectAccessAttribute
{
    public class ProjectAccessAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "ProjectAccess_";
        public ProjectAccessAttribute() => Policy = $"{PolicyPrefix}Any";
        public ProjectAccessAttribute(ProjectRole role) => Policy = $"{PolicyPrefix}{role}";
    }
}
