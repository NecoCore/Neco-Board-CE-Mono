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
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var routeData = httpContext?.GetRouteData();
            var taskId = routeData?.Values["taskId"]?.ToString();
            var columnId = routeData?.Values["columnId"]?.ToString();
            var projectId = routeData?.Values["projectId"]?.ToString() ?? routeData?.Values["id"]?.ToString();

            CheckResult result = new() { Result = false };

            if (string.IsNullOrEmpty(userId)) return;

            if (!string.IsNullOrEmpty(taskId))
            {
                result = requirement.Role.HasValue ?
                    await _userAccess.HasAccessToTask(userId, taskId, requirement.Role.Value) :
                    await _userAccess.HasAccessToTask(userId, taskId);
            }
            else if (!string.IsNullOrEmpty(columnId))
            {
                result = requirement.Role.HasValue ?
                    await _userAccess.HasAccessToColumn(userId, columnId, requirement.Role.Value) :
                    await _userAccess.HasAccessToColumn(userId, columnId);
            }
            else if (!string.IsNullOrEmpty(projectId))
            {
                result = requirement.Role.HasValue ?
                    await _userAccess.HasAccessToProject(userId, projectId, requirement.Role.Value) :
                    await _userAccess.HasAccessToProject(userId, projectId);
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
