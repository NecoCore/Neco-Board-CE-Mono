using Microsoft.AspNetCore.Authorization;
using neco_board_ce.Models.Results;
using neco_board_ce.Utils.Check;
using System.Security.Claims;

namespace neco_board_ce.Attributes.ProjectAccessAttribute
{
    public class ProjectAccessHandler : AuthorizationHandler<ProjectAccessRequirement>
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserAccessCheck _userAccess;

        public ProjectAccessHandler(IHttpContextAccessor contextAccessor, UserAccessCheck userAccess)
        {
            _contextAccessor = contextAccessor;
            _userAccess = userAccess;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectAccessRequirement requirement)
        {
            var httpContext = _contextAccessor.HttpContext;
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return;

            var routeData = httpContext?.GetRouteData();
            
            var taskIdStr = routeData?.Values["taskId"]?.ToString();
            var columnIdStr = routeData?.Values["columnId"]?.ToString();
            var projectIdStr = routeData?.Values["projectId"]?.ToString() ?? routeData?.Values["id"]?.ToString();

            Guid? taskId = Guid.TryParse(taskIdStr, out var tId) ? tId : null;
            Guid? columnId = Guid.TryParse(columnIdStr, out var cId) ? cId : null;
            Guid? projectId = Guid.TryParse(projectIdStr, out var pId) ? pId : null;

            CheckResult result = new() { Result = false };

            if (taskId.HasValue)
            {
                result = requirement.Role.HasValue ?
                    await _userAccess.HasAccessToTask(userId, taskId.Value, requirement.Role.Value) :
                    await _userAccess.HasAccessToTask(userId, taskId.Value);
            }
            else if (columnId.HasValue)
            {
                result = requirement.Role.HasValue ?
                    await _userAccess.HasAccessToColumn(userId, columnId.Value, requirement.Role.Value) :
                    await _userAccess.HasAccessToColumn(userId, columnId.Value);
            }
            else if (projectId.HasValue)
            {
                result = requirement.Role.HasValue ?
                    await _userAccess.HasAccessToProject(userId, projectId.Value, requirement.Role.Value) :
                    await _userAccess.HasAccessToProject(userId, projectId.Value);
            }

            var userGlobalRole = context.User.FindFirstValue(ClaimTypes.Role);
            bool isGlobalAdmin = userGlobalRole == "ADMIN" || userGlobalRole == "OWNER";

            if(result.Result || isGlobalAdmin)
            {
                httpContext?.Items["ProjectId"] = result.ProjectId;
                context.Succeed(requirement);
            }
        }
    }
}
