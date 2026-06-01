using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Utils.Check;
using System.Collections.Concurrent;

namespace neco_board_ce.Controllers.Hubs
{
    [Authorize]
    public class AppHub : Hub<IAppClient>
    {
        private readonly ILogger<AppHub> _logger;
        private readonly UserAccessCheck _userAccess;
        private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineUsers = new();

        public AppHub(ILogger<AppHub> logger, UserAccessCheck userAccess)
        {
            _logger = logger;
            _userAccess = userAccess;
        }

        public async Task JoinProject(string projectId)
        {
            var userId = Context.UserIdentifier;
            if (userId is null) return;
            var chek = await _userAccess.HasAccessToProject(userId, projectId);
            if (!chek.Result) throw new HubException("Access denied");
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Project(projectId));
        }

        public Task LeaveProject(string projectId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Project(projectId));

        public async Task JoinTask(string taskId)
        {
            var userId = Context.UserIdentifier;
            if (userId is null) return;
            var chek = await _userAccess.HasAccessToTask(userId, taskId);
            if (!chek.Result) throw new HubException("Access denied");
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Task(taskId));
        }

        public Task LeaveTask(string taskId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Task(taskId));

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User conecting...");
            var userId = Context.UserIdentifier;

            if(userId != null)
            {
                _logger.LogInformation("Add user {userId} in public group", userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.All);

                if (Context.User != null && (Context.User.IsInRole(Constants.ROLE_ADMIN) || Context.User.IsInRole(Constants.ROLE_OWNER)))
                {
                    _logger.LogInformation("Add user {userId} in admin group", userId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Admins);
                }

                bool isNewUserOnline = false;

                _onlineUsers.AddOrUpdate(userId,
                    addValueFactory: key =>
                    {
                        isNewUserOnline = true;
                        _logger.LogInformation("User {userId} now is online", userId);
                        return new HashSet<string> { Context.ConnectionId };
                    },
                    updateValueFactory: (key, connections) => 
                    { 
                        lock(connections) 
                        {
                            connections.Add(Context.ConnectionId);
                        }
                        _logger.LogInformation("Updetaed online users");
                        return connections;
                    }
                );

                if(isNewUserOnline)
                {
                    _logger.LogInformation("Notificate {userId} now is online", userId);
                    await Clients.Others.UserConnected(userId);
                }
            } else
            {
                _logger.LogWarning("Failed connection. User not autorized");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconecting...");
            var userId = Context.UserIdentifier;

            if (userId != null)
            {
                bool isUserOffline = false;

                if (_onlineUsers.TryGetValue(userId, out var connections))
                {
                    lock (connections)
                    {
                        _logger.LogInformation("Removing user {userId} from online...", userId);
                        connections.Remove(Context.ConnectionId);

                        if (connections.Count == 0)
                        {
                            isUserOffline = true;
                        }
                    }

                    if (isUserOffline)
                    {
                        _logger.LogInformation("Notificate user {userId} now ofline", userId);
                        _onlineUsers.TryRemove(userId, out _);
                        await Clients.All.UserDisconnected(userId);
                    }
                }
            } else
            {
                _logger.LogWarning("Failed disconnectiond. User not autorized");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<IEnumerable<string>> GetOnlineUsers()
        {
            return Task.FromResult(_onlineUsers.Keys.AsEnumerable());
        }
    }
}
