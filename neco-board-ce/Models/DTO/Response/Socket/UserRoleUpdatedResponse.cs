using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Response.Socket
{
    /// <summary>
    /// Payload of the <c>UserRoleUpdatedInProject</c> socket event.
    /// </summary>
    public class UserRoleUpdatedResponse
    {
        public Guid UserId { get; set; }
        public ProjectRole Role { get; set; }
    }
}
