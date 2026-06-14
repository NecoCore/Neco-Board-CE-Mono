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


        public Task ProjectUpdated(Guid projectId, string projectName)
        {
            var payload = new ProjectUpdatedResponse { Id = projectId, Name = projectName };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Project(projectId.ToString())).ProjectUpdated(payload),
                _hub.Clients.Group(HubGroups.Admins).ProjectUpdated(payload)
            );
        }

        public Task ProjectDeleted(Guid projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).ProjectDeleted(projectId),
            _hub.Clients.Group(HubGroups.Admins).ProjectDeleted(projectId)
        );
        #endregion

        #region Users in projects
        public Task ProjectAddUser(Guid projectId, Guid userId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).UserAddedToProject(userId),
            _hub.Clients.Group(HubGroups.Admins).UserAddedToProject(userId)
        );

        public Task ProjectUpdateUser(Guid projectId, Guid userId, ProjectRole newRole)
        {
            var payload = new UserRoleUpdatedResponse { UserId = userId, Role = newRole };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Project(projectId.ToString())).UserRoleUpdatedInProject(payload),
                _hub.Clients.Group(HubGroups.Admins).UserRoleUpdatedInProject(payload)
            );
        }

        public Task ProjectRemoveUser(Guid projectId, Guid userId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).UserRemovedFromProject(userId),
            _hub.Clients.Group(HubGroups.Admins).UserRemovedFromProject(userId)
        );
        #endregion

        #region Columns
        public Task ColumnCreated(Guid projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).ColumnCreated());

        public Task ColumnUpdated(Guid projectId, Guid columnId, string columnName) =>
            _hub.Clients.Group(HubGroups.Project(projectId.ToString()))
                .ColumnUpdated(new ColumnUpdatedResponse { ColumnId = columnId, Name = columnName });

        public Task ColumnOrderUpdated(Guid projectId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).ColumnUpdatedOrder());

        public Task ColumnDelete(Guid projectId, Guid columnId) => Task.WhenAll(
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).ColumnDeleted(columnId));
        #endregion

        #region Tasks
        public Task TaskCreated(Guid projectId, Guid columnId) =>
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).TaskCreated(columnId);

        public Task TaskUpdated(Guid projectId, Guid taskId) =>
            _hub.Clients.Group(HubGroups.Project(projectId.ToString())).TaskUpdated(taskId);

        public Task TaskColumnUpdated(Guid projectId, Guid oldColumnId, Guid newColumnId) =>
            _hub.Clients.Group(HubGroups.Project(projectId.ToString()))
                .TaskColumnUpdated(new TaskColumnUpdatedResponse { OldColumnId = oldColumnId, NewColumnId = newColumnId });

        public Task TaskStatusUpdated(Guid projectId, Guid taskId, Guid columnId, ColumnTaskStatus newStatus)
        {
            var payload = new TaskStatusUpdatedResponse { TaskId = taskId, ColumnId = columnId, Status = newStatus };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskStatusUpdated(payload),
                _hub.Clients.Group(HubGroups.Project(projectId.ToString())).TaskStatusUpdated(payload));
        }

        public Task TaskPriorityUpdated(Guid projectId, Guid taskId, Guid columnId, TaskPriority newPriority)
        {
            var payload = new TaskPriorityUpdatedResponse { TaskId = taskId, ColumnId = columnId, Priority = newPriority };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskPriorityUpdated(payload),
                _hub.Clients.Group(HubGroups.Project(projectId.ToString())).TaskPriorityUpdated(payload));
        }

        public Task TaskDelete(Guid projectId, Guid columnId, Guid taskId)
        {
            var payload = new TaskDeletedResponse { TaskId = taskId, ColumnId = columnId };
            return Task.WhenAll(
                _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskDeleted(payload),
                _hub.Clients.Group(HubGroups.Project(projectId.ToString())).TaskDeleted(payload));
        }
        #endregion

        #region Users in task
        public Task TaskAddUser(Guid taskId) =>
            _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskUserAdded();

        public Task TaskRemoveUser(Guid taskId, Guid userId) =>
            _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskUserRemoved(userId);
        #endregion

        #region Task attachments
        public Task TaskAttachmentUploaded(Guid taskId) =>
            _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskAttachmentUploaded();

        public Task TaskAttachmentDeleted(Guid taskId, Guid attachmentId) =>
            _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskAttachmentDeleted(attachmentId);
        #endregion

        #region Task images
        public Task TaskImageUploaded(Guid taskId) =>
            _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskImageUploaded();

        public Task TaskImageDeleted(Guid taskId, Guid imageId) =>
            _hub.Clients.Group(HubGroups.Task(taskId.ToString())).TaskImageDeleted(imageId);
        #endregion
    }
}
