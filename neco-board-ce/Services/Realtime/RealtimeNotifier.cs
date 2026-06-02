using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Services.Realtime
{
    /// <summary>
    /// Sends real-time events to clients via the strongly-typed <see cref="AppHub"/>.
    /// Controllers depend on <see cref="IRealtimeNotifier"/> and never touch SignalR directly:
    /// routing (groups / users) lives here, payloads are defined by <see cref="IAppClient"/>.
    /// </summary>
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<AppHub, IAppClient> _hub;

        public RealtimeNotifier(IHubContext<AppHub, IAppClient> hub) => _hub = hub;


        #region Projects
        public Task ProjectCreated() =>
            _hub.Clients.Group(HubGroups.Admins).ProjectCreated();


        public Task ProjectUpdated(string projectId, string projectName) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ProjectUpdated(projectId, projectName),
            _hub.Clients.Group(HubGroups.Admins).ProjectUpdated(projectId, projectName)
        );

        public Task ProjectDeleted(string projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ProjectDeleted(projectId),
            _hub.Clients.Group(HubGroups.Admins).ProjectDeleted(projectId)
        );
        #endregion

        #region Users in projects
        public Task ProjectAddUser(string projectId, string userId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).UserAddedToProject(userId),
            _hub.Clients.Group(HubGroups.Admins).UserAddedToProject(userId)
        );

        public Task ProjectUpdateUser(string projectId, string userId, ProjectRole newRole) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).UserRoleUpdatedInProject(userId, newRole),
            _hub.Clients.Group(HubGroups.Admins).UserRoleUpdatedInProject(userId, newRole)
        );

        public Task ProjectRemoveUser(string projectId, string userId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).UserRemovedFromProject(userId),
            _hub.Clients.Group(HubGroups.Admins).UserRemovedFromProject(userId)
        );
        #endregion

        #region Columns
        public Task ColumnCreated(string projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ColumnCreated());

        public Task ColumnUpdated(string projectId, string columnId, string columnName) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ColumnUpdated(columnId, columnName));

        public Task ColumnOrderUpdated(string projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ColumnUpdatedOrder());

        public Task ColumnDelete(string projectId, string columnId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ColumnDeleted(columnId));
        #endregion

        #region Tasks
        public Task TaskCreated(string projectId, string columnId) =>
            _hub.Clients.Group(HubGroups.Project(projectId)).TaskCreated(columnId);

        public Task TaskUpdated(string projectId, string taskId) =>
            _hub.Clients.Group(HubGroups.Project(projectId)).TaskUpdated(taskId);

        public Task TaskColumnUpdated(string projectId, string oldColumnId, string newColumnId) =>
            _hub.Clients.Group(HubGroups.Project(projectId)).TaskColumnUpdated(oldColumnId, newColumnId);

        public Task TaskStatusUpdated(string projectId, string taskId, string columnId, ColumnTaskStatus newStatus) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskStatusUpdated(taskId, columnId, newStatus),
            _hub.Clients.Group(HubGroups.Project(projectId)).TaskStatusUpdated(taskId, columnId, newStatus));

        public Task TaskPriorityUpdated(string projectId, string taskId, string columnId, TaskPriority newPriority) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskPriorityUpdated(taskId, columnId, newPriority),
            _hub.Clients.Group(HubGroups.Project(projectId)).TaskPriorityUpdated(taskId, columnId, newPriority));

        public Task TaskDelete(string projectId, string columnId, string taskId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskDeleted(taskId, columnId),
            _hub.Clients.Group(HubGroups.Project(projectId)).TaskDeleted(taskId, columnId));
        #endregion

        #region Users in task
        public Task TaskAddUser(string taskId) =>
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskUserAdded();

        public Task TaskRemoveUser(string taskId, string userId) =>
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskUserRemoved(userId);
        #endregion
    }
}
