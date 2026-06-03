using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Response.Socket;
using Saunter.Attributes;

namespace neco_board_ce.Controllers.Hubs
{
    /// <summary>
    /// AsyncAPI documentation surface for the server-&gt;client socket events defined by
    /// <see cref="IAppClient"/>. Saunter cannot annotate an interface, so this class carries the
    /// <c>[AsyncApi]</c> / <c>[Channel]</c> / <c>[PublishOperation]</c> attributes instead.
    /// </summary>
    /// <remarks>
    /// This type is never instantiated or called — it exists purely for the AsyncAPI generator
    /// served at <c>/asyncapi/asyncapi.json</c> (UI at <c>/docs/socket</c>). Implementing
    /// <see cref="IAppClient"/> makes the compiler keep this list in sync with the real contract.
    /// Server-&gt;client events are modelled as <c>publish</c> operations (the server publishes;
    /// the client subscribes). Each operation's <c>Description</c> names the triggering REST route.
    /// Every operation declares an explicit message type so the document never emits a null message
    /// (events without a payload use <see cref="EmptyPayload"/>).
    /// </remarks>
    [AsyncApi]
    public class AppSocketDocs : IAppClient
    {
        #region Projects

        [Channel("ProjectCreated")]
        [PublishOperation(typeof(EmptyPayload), Summary = "Project created",
            Description = "Triggered by POST /api/project (author + admins) and POST /api/project/{projectId}/users (added user). No payload.")]
        public Task ProjectCreated() => Task.CompletedTask;

        [Channel("ProjectUpdated")]
        [PublishOperation(typeof(ProjectUpdatedResponse), Summary = "Project updated",
            Description = "Triggered by PUT /api/project/{id}. Sent to project:{id} and admins.")]
        public Task ProjectUpdated(ProjectUpdatedResponse payload) => Task.CompletedTask;

        [Channel("ProjectDeleted")]
        [PublishOperation(typeof(string), Summary = "Project deleted",
            Description = "Triggered by DELETE /api/project/{id}. Sent to project:{id} and the all group. Payload: project id.")]
        public Task ProjectDeleted(string id) => Task.CompletedTask;

        #endregion

        #region Project membership

        [Channel("UserAddedToProject")]
        [PublishOperation(typeof(string), Summary = "User added to project",
            Description = "Triggered by POST /api/project/{projectId}/users. Sent to project:{id}. Payload: user id.")]
        public Task UserAddedToProject(string userId) => Task.CompletedTask;

        [Channel("UserRoleUpdatedInProject")]
        [PublishOperation(typeof(UserRoleUpdatedResponse), Summary = "User role updated",
            Description = "Triggered by PATCH /api/project/{projectId}/users/{userId}. Sent to project:{id} and the affected user.")]
        public Task UserRoleUpdatedInProject(UserRoleUpdatedResponse payload) => Task.CompletedTask;

        [Channel("UserRemovedFromProject")]
        [PublishOperation(typeof(string), Summary = "User removed from project",
            Description = "Triggered by DELETE /api/project/{projectId}/users/{userId}. Sent to project:{id} and the affected user. Payload: user id.")]
        public Task UserRemovedFromProject(string userId) => Task.CompletedTask;

        #endregion

        #region Columns

        [Channel("ColumnCreated")]
        [PublishOperation(typeof(EmptyPayload), Summary = "Column created",
            Description = "Triggered by POST /api/column/in-project/{projectId}. Sent to project:{id}. No payload.")]
        public Task ColumnCreated() => Task.CompletedTask;

        [Channel("ColumnUpdated")]
        [PublishOperation(typeof(ColumnUpdatedResponse), Summary = "Column updated",
            Description = "Triggered by PUT /api/column/{columnId}. Sent to project:{id}.")]
        public Task ColumnUpdated(ColumnUpdatedResponse payload) => Task.CompletedTask;

        [Channel("ColumnUpdatedOrder")]
        [PublishOperation(typeof(EmptyPayload), Summary = "Column order changed",
            Description = "Triggered by PUT /api/column/{columnId}/order. Sent to project:{id}. No payload.")]
        public Task ColumnUpdatedOrder() => Task.CompletedTask;

        [Channel("ColumnDeleted")]
        [PublishOperation(typeof(string), Summary = "Column deleted",
            Description = "Triggered by DELETE /api/column/{columnId}. Sent to project:{id}. Payload: column id.")]
        public Task ColumnDeleted(string columnId) => Task.CompletedTask;

        #endregion

        #region Tasks (board level)

        [Channel("TaskCreated")]
        [PublishOperation(typeof(string), Summary = "Task created",
            Description = "Triggered by POST /api/tasks. Sent to project:{id}. Payload: column id.")]
        public Task TaskCreated(string columnId) => Task.CompletedTask;

        [Channel("TaskUpdated")]
        [PublishOperation(typeof(string), Summary = "Task updated",
            Description = "Triggered by PUT /api/tasks/{taskId}. Sent to project:{id}. Payload: task id.")]
        public Task TaskUpdated(string id) => Task.CompletedTask;

        [Channel("TaskColumnUpdated")]
        [PublishOperation(typeof(TaskColumnUpdatedResponse), Summary = "Task moved between columns",
            Description = "Triggered by PATCH /api/tasks/{taskId}/column. Sent to project:{id}.")]
        public Task TaskColumnUpdated(TaskColumnUpdatedResponse payload) => Task.CompletedTask;

        [Channel("TaskStatusUpdated")]
        [PublishOperation(typeof(TaskStatusUpdatedResponse), Summary = "Task status changed",
            Description = "Triggered by PATCH /api/tasks/{taskId}/status. Sent to task:{taskId} and project:{id}.")]
        public Task TaskStatusUpdated(TaskStatusUpdatedResponse payload) => Task.CompletedTask;

        [Channel("TaskPriorityUpdated")]
        [PublishOperation(typeof(TaskPriorityUpdatedResponse), Summary = "Task priority changed",
            Description = "Triggered by PATCH /api/tasks/{taskId}/priority. Sent to task:{taskId} and project:{id}.")]
        public Task TaskPriorityUpdated(TaskPriorityUpdatedResponse payload) => Task.CompletedTask;

        [Channel("TaskDeleted")]
        [PublishOperation(typeof(TaskDeletedResponse), Summary = "Task deleted",
            Description = "Triggered by DELETE /api/tasks/{taskId}. Sent to task:{taskId} and project:{id}.")]
        public Task TaskDeleted(TaskDeletedResponse payload) => Task.CompletedTask;

        #endregion

        #region Task assignees

        [Channel("TaskUserAdded")]
        [PublishOperation(typeof(EmptyPayload), Summary = "User assigned to task",
            Description = "Triggered by POST /api/tasks/{taskId}/user. Sent to task:{taskId}. No payload.")]
        public Task TaskUserAdded() => Task.CompletedTask;

        [Channel("TaskUserRemoved")]
        [PublishOperation(typeof(string), Summary = "User removed from task",
            Description = "Triggered by DELETE /api/tasks/{taskId}/user. Sent to task:{taskId}. Payload: user id.")]
        public Task TaskUserRemoved(string userId) => Task.CompletedTask;

        #endregion

        #region Presence (hub lifecycle, not REST)

        [Channel("UserConnected")]
        [PublishOperation(typeof(string), Summary = "User came online",
            Description = "Emitted by the hub on connect (not REST). Sent to all other clients. Payload: user id.")]
        public Task UserConnected(string userId) => Task.CompletedTask;

        [Channel("UserDisconnected")]
        [PublishOperation(typeof(string), Summary = "User went offline",
            Description = "Emitted by the hub on disconnect (not REST). Sent to all clients. Payload: user id.")]
        public Task UserDisconnected(string userId) => Task.CompletedTask;

        #endregion
    }
}
