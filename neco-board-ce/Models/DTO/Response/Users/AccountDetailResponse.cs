using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Users
{
    public class AccountDetailResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public WorkspaceRoles Role { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public int ProjectCount { get; set; }
        public int AssignedTaskCount { get; set; }

        public AccountDetailResponse() { }

        public AccountDetailResponse(Account account)
        {
            Id = account.Id;
            Name = account.Name;
            Avatar = account.Avatar;
            Role = account.Role;
            CreatedAt = account.CreatedAt;
            ProjectCount = account.Projects.Count;
            AssignedTaskCount = account.Tasks.Count;
        }
    }
}
