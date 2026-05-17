using Microsoft.AspNetCore.SignalR;

namespace neco_board_ce.Controllers.Hubs
{
    public class TaskHub : Hub
    {
        public async Task JoinTask(string taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, taskId);
        }

        public async Task LeaveTask(string taskId) 
        { 
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, taskId);
        }
    }
}
