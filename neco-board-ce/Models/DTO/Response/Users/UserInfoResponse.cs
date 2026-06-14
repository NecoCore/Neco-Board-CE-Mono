using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Users
{
    public class UserInfoResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public WorkspaceRoles Role { get; set; }

        public UserInfoResponse(Account account) 
        {
            Id = account.Id;
            Name = account.Name;
            Avatar = account.Avatar;
            Role = account.Role;
        }
    }
}
