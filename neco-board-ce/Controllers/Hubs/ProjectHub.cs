using Microsoft.AspNetCore.SignalR;

namespace neco_board_ce.Controllers.Hubs
{
    public class ProjectHub : Hub
    {
        public async Task JoinProject(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
        }

        public async Task LeaveProject(string projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
        }
    }
}
