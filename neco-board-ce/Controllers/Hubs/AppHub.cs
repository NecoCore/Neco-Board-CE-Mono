using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Utils.Check;
using System.Collections.Concurrent;

namespace neco_board_ce.Controllers.Hubs
{
    /// <summary>
    /// SignalR Hub for handling real-time communications within the workspace.
    /// </summary>
    /// <remarks>
    /// Manages project-specific and task-specific groups, connection lifecycle, 
    /// and user online/offline status tracking. All methods require authentication.
    /// </remarks>
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

        /// <summary>
        /// Adds the authenticated user's connection to a SignalR group for a specific project.
        /// </summary>
        /// <remarks>
        /// Verifies that the user has access to the project before adding them to the group.
        /// Throws a <see cref="HubException"/> if access is denied.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <exception cref="HubException">Thrown when the user lacks access to the project.</exception>
        public async Task JoinProject(Guid projectId)
        {
            var userIdStr = Context.UserIdentifier;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId)) return;
            var check = await _userAccess.HasAccessToProject(userId, projectId);
            if (!check.Result) throw new HubException("Access denied");
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Project(projectId.ToString()));
        }

        /// <summary>
        /// Removes the authenticated user's connection from a SignalR group for a specific project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        public Task LeaveProject(Guid projectId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Project(projectId.ToString()));

        /// <summary>
        /// Adds the authenticated user's connection to a SignalR group for a specific task.
        /// </summary>
        /// <remarks>
        /// Verifies that the user has access to the task before adding them to the group.
        /// Throws a <see cref="HubException"/> if access is denied.
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <exception cref="HubException">Thrown when the user lacks access to the task.</exception>
        public async Task JoinTask(Guid taskId)
        {
            var userIdStr = Context.UserIdentifier;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId)) return;
            var check = await _userAccess.HasAccessToTask(userId, taskId);
            if (!check.Result) throw new HubException("Access denied");
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Task(taskId.ToString()));
        }

        /// <summary>
        /// Removes the authenticated user's connection from a SignalR group for a specific task.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        public Task LeaveTask(Guid taskId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Task(taskId.ToString()));

        /// <summary>
        /// Handles the connection event for a SignalR client.
        /// </summary>
        /// <remarks>
        /// Performs the following actions:
        /// <list type="bullet">
        ///   <item><description>Adds the connection to the global 'All' group.</description></item>
        ///   <item><description>Adds admins/owners to the 'Admins' group.</description></item>
        ///   <item><description>Tracks the user's connection ID for online status.</description></item>
        ///   <item><description>Broadcasts <c>UserConnected</c> to others if this is the user's first connection.</description></item>
        /// </list>
        /// </remarks>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User connecting...");
            var userId = Context.UserIdentifier;

            if(userId != null)
            {
                _logger.LogInformation("Add user {userId} in public group", userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.All);

                if (Context.User != null && (Context.User.IsInRole(Constants.Roles.Admin) || Context.User.IsInRole(Constants.Roles.Owner)))
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
                        _logger.LogInformation("Updated online users");
                        return connections;
                    }
                );

                if(isNewUserOnline)
                {
                    _logger.LogInformation("Notificate {userId} now is online", userId);
                    await Clients.Others.UserConnected(Guid.Parse(userId));
                }
            } else
            {
                _logger.LogWarning("Failed connection. User not authorized");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handles the disconnection event for a SignalR client.
        /// </summary>
        /// <remarks>
        /// Removes the connection ID from tracking. If no more connections exist for the user,
        /// broadcasts <c>UserDisconnected</c> to all clients.
        /// </remarks>
        /// <param name="exception">Optional exception that occurred during disconnection.</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconnecting...");
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
                        _logger.LogInformation("Notificate user {userId} now offline", userId);
                        _onlineUsers.TryRemove(userId, out _);
                        await Clients.All.UserDisconnected(Guid.Parse(userId));
                    }
                }
            } else
            {
                _logger.LogWarning("Failed disconnection. User not authorized");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Retrieves the list of currently online user IDs.
        /// </summary>
        /// <returns>An enumerable collection of user ID strings.</returns>
        public Task<IEnumerable<string>> GetOnlineUsers()
        {
            return Task.FromResult(_onlineUsers.Keys.AsEnumerable());
        }
    }
}
