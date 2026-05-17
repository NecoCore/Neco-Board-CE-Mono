using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Data;
using System.Collections.Concurrent;

namespace neco_board_ce.Controllers.Hubs
{
    public class AppHub : Hub
    {
        private readonly ILogger<AppHub> _logger;
        private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineUsers = new();

        public AppHub(ILogger<AppHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User conecting...");
            var userId = Context.UserIdentifier;

            if(userId != null)
            {
                _logger.LogInformation("Add user {userId} in public group", userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, Constants.GROUP_ALL);

                if (Context.User != null && (Context.User.IsInRole(Constants.ROLE_ADMIN) || Context.User.IsInRole(Constants.ROLE_OWNER)))
                {
                    _logger.LogInformation("Add user {userId} in admin group", userId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, Constants.GROUP_ADMINS);
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
                    await Clients.Others.SendAsync(Constants.SOKET_EVENT_USER_CONNECT, userId);
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
                        await Clients.All.SendAsync(Constants.SOKET_EVENT_USER_DISCONNECT, userId);
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
