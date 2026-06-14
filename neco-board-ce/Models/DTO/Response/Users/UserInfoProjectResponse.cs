using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Users
{
    public class UserInfoProjectResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public ProjectRole Role { get; set; }

        public UserInfoProjectResponse(UserProjectRole account)
        {
            Id = account.User.Id;
            Name = account.User.Name;
            Avatar = account.User.Avatar;
            Role = account.Role;
        }
    }
}
