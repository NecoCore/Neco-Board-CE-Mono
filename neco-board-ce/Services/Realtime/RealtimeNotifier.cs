using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Response.Socket;
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


        public Task ProjectUpdated(string projectId, string projectName)
        {
            var payload = new ProjectUpdatedResponse { Id = projectId, Name = projectName };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Project(projectId)).ProjectUpdated(payload),
                _hub.Clients.Group(HubGroups.Admins).ProjectUpdated(payload)
            );
        }

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

        public Task ProjectUpdateUser(string projectId, string userId, ProjectRole newRole)
        {
            var payload = new UserRoleUpdatedResponse { UserId = userId, Role = newRole };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Project(projectId)).UserRoleUpdatedInProject(payload),
                _hub.Clients.Group(HubGroups.Admins).UserRoleUpdatedInProject(payload)
            );
        }

        public Task ProjectRemoveUser(string projectId, string userId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).UserRemovedFromProject(userId),
            _hub.Clients.Group(HubGroups.Admins).UserRemovedFromProject(userId)
        );
        #endregion

        #region Columns
        public Task ColumnCreated(string projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId)).ColumnCreated());

        public Task ColumnUpdated(string projectId, string columnId, string columnName) =>
            _hub.Clients.Group(HubGroups.Project(projectId))
                .ColumnUpdated(new ColumnUpdatedResponse { ColumnId = columnId, Name = columnName });

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
            _hub.Clients.Group(HubGroups.Project(projectId))
                .TaskColumnUpdated(new TaskColumnUpdatedResponse { OldColumnId = oldColumnId, NewColumnId = newColumnId });

        public Task TaskStatusUpdated(string projectId, string taskId, string columnId, ColumnTaskStatus newStatus)
        {
            var payload = new TaskStatusUpdatedResponse { TaskId = taskId, ColumnId = columnId, Status = newStatus };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Task(taskId)).TaskStatusUpdated(payload),
                _hub.Clients.Group(HubGroups.Project(projectId)).TaskStatusUpdated(payload));
        }

        public Task TaskPriorityUpdated(string projectId, string taskId, string columnId, TaskPriority newPriority)
        {
            var payload = new TaskPriorityUpdatedResponse { TaskId = taskId, ColumnId = columnId, Priority = newPriority };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Task(taskId)).TaskPriorityUpdated(payload),
                _hub.Clients.Group(HubGroups.Project(projectId)).TaskPriorityUpdated(payload));
        }

        public Task TaskDelete(string projectId, string columnId, string taskId)
        {
            var payload = new TaskDeletedResponse { TaskId = taskId, ColumnId = columnId };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Task(taskId)).TaskDeleted(payload),
                _hub.Clients.Group(HubGroups.Project(projectId)).TaskDeleted(payload));
        }
        #endregion

        #region Users in task
        public Task TaskAddUser(string taskId) =>
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskUserAdded();

        public Task TaskRemoveUser(string taskId, string userId) =>
            _hub.Clients.Group(HubGroups.Task(taskId)).TaskUserRemoved(userId);
        #endregion
    }
}
