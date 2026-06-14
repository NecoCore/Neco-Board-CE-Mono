using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Attributes.ProjectAccessAttribute
{
    public class ProjectAccessPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public ProjectAccessPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(ProjectAccessAttribute.PolicyPrefix))
            {
                var roleStr = policyName.Substring(ProjectAccessAttribute.PolicyPrefix.Length);
                ProjectRole? role = null;

                if (Enum.TryParse<ProjectRole>(roleStr, true, out var parsedRole))
                {
                    role = parsedRole;
                }

                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new ProjectAccessRequirement(role));
                return policy.Build();
            }

            return await base.GetPolicyAsync(policyName);
        }
    }
}
