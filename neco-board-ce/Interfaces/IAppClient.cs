using neco_board_ce.Controllers.API;
using neco_board_ce.Models.DTO.Response.Socket;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Interfaces
{
    /// <summary>
    /// Strongly-typed contract of all real-time events the server pushes to clients over SignalR.
    /// </summary>
    /// <remarks>
    /// The method name equals the event name on the client; the parameters form the payload.
    /// Routing (which group / which user receives the event) is NOT part of this contract —
    /// it lives in <see cref="IRealtimeNotifier"/>, which strips routing-only ids (project id,
    /// the column id used to pick a group) and forwards the rest here.
    /// <para>
    /// AsyncAPI documentation for these events is generated from
    /// <see cref="neco_board_ce.Controllers.Hubs.AppSocketDocs"/> (Saunter cannot annotate an
    /// interface). The XML comments here feed IntelliSense / DocFX.
    /// </para>
    /// <para>
    /// Enum payloads (<see cref="ColumnTaskStatus"/>, <see cref="TaskPriority"/>,
    /// <see cref="ProjectRole"/>) are serialized as strings (<c>JsonStringEnumConverter</c> via
    /// <c>AddJsonProtocol</c>).
    /// </para>
    /// </remarks>
    public interface IAppClient
    {
        #region Projects

        /// <summary>A new project was created.</summary>
        /// <remarks>
        /// Source: <see cref="ProjectController"/> (<c>POST /api/project</c>) → author
        /// (<c>User(self)</c>) and the admins group; also
        /// <see cref="UserProjectController"/> (<c>POST /api/project/{projectId}/users</c>)
        /// → the added user (<c>User(userId)</c>). No payload — the client refetches its project list.
        /// </remarks>
        Task ProjectCreated();

        /// <summary>A project was updated.</summary>
        /// <remarks>
        /// Source: <see cref="ProjectController"/> (<c>PUT /api/project/{id}</c>) →
        /// project group <c>project:{id}</c> and the admins group.
        /// </remarks>
        /// <param name="payload">Updated project id and name.</param>
        Task ProjectUpdated(ProjectUpdatedResponse payload);

        /// <summary>A project was deleted.</summary>
        /// <remarks>
        /// Source: <see cref="ProjectController"/> (<c>DELETE /api/project/{id}</c>) →
        /// project group <c>project:{id}</c> and the <c>all</c> group.
        /// </remarks>
        /// <param name="id">Id of the deleted project.</param>
        Task ProjectDeleted(Guid id);

        #endregion

        #region Project membership (project:{id} group + the affected user)

        /// <summary>A user was added to a project.</summary>
        /// <remarks>Source: <see cref="UserProjectController"/> (<c>POST /api/project/{projectId}/users</c>) → project group.</remarks>
        /// <param name="userId">Id of the added user.</param>
        Task UserAddedToProject(Guid userId);

        /// <summary>A user's role in a project changed.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>PATCH /api/project/{projectId}/users/{userId}</c>)
        /// → project group and the affected user.
        /// </remarks>
        /// <param name="payload">Affected user id and the new project role.</param>
        Task UserRoleUpdatedInProject(UserRoleUpdatedResponse payload);

        /// <summary>A user was removed from a project.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>DELETE /api/project/{projectId}/users/{userId}</c>)
        /// → project group and the affected user.
        /// </remarks>
        /// <param name="userId">Id of the removed user.</param>
        Task UserRemovedFromProject(Guid userId);

        #endregion

        #region Columns (always to the project:{id} group)

        /// <summary>A column was created.</summary>
        /// <remarks>Source: <see cref="ColumnsProjectController"/> (<c>POST /api/column/in-project/{projectId}</c>). No payload — the client refetches the column list.</remarks>
        Task ColumnCreated();

        /// <summary>A column was updated (renamed).</summary>
        /// <remarks>Source: <see cref="ColumnsProjectController"/> (<c>PUT /api/column/{columnId}</c>).</remarks>
        /// <param name="payload">Updated column id and name.</param>
        Task ColumnUpdated(ColumnUpdatedResponse payload);

        /// <summary>Column order changed.</summary>
        /// <remarks>Source: <see cref="ColumnsProjectController"/> (<c>PUT /api/column/{columnId}/order</c>). No payload — the client refetches the column order.</remarks>
        Task ColumnUpdatedOrder();

        /// <summary>A column was deleted.</summary>
        /// <remarks>Source: <see cref="ColumnsProjectController"/> (<c>DELETE /api/column/{columnId}</c>).</remarks>
        /// <param name="columnId">Id of the deleted column.</param>
        Task ColumnDeleted(Guid columnId);

        #endregion

        #region Tasks (board level, project:{id} group)

        /// <summary>A task was created.</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>POST /api/tasks</c>).</remarks>
        /// <param name="columnId">Id of the column the task was created in.</param>
        Task TaskCreated(Guid columnId);

        /// <summary>A task was updated (content fields).</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>PUT /api/tasks/{taskId}</c>).</remarks>
        /// <param name="id">Id of the updated task.</param>
        Task TaskUpdated(Guid id);

        /// <summary>A task was moved between columns.</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>PATCH /api/tasks/{taskId}/column</c>) → project group.</remarks>
        /// <param name="payload">Source and destination column ids.</param>
        Task TaskColumnUpdated(TaskColumnUpdatedResponse payload);

        /// <summary>Task status changed.</summary>
        /// <remarks>
        /// Source: <see cref="TaskInfoController"/> (<c>PATCH /api/tasks/{taskId}/status</c>) →
        /// task group <c>task:{taskId}</c> and project group <c>project:{id}</c>.
        /// </remarks>
        /// <param name="payload">Task id, its column id and the new status.</param>
        Task TaskStatusUpdated(TaskStatusUpdatedResponse payload);

        /// <summary>Task priority changed.</summary>
        /// <remarks>
        /// Source: <see cref="TaskInfoController"/> (<c>PATCH /api/tasks/{taskId}/priority</c>) →
        /// task group and project group.
        /// </remarks>
        /// <param name="payload">Task id, its column id and the new priority.</param>
        Task TaskPriorityUpdated(TaskPriorityUpdatedResponse payload);

        /// <summary>A task was deleted.</summary>
        /// <remarks>
        /// Source: <see cref="TaskColumnController"/> (<c>DELETE /api/tasks/{taskId}</c>) →
        /// task group <c>task:{taskId}</c> and project group <c>project:{id}</c>.
        /// </remarks>
        /// <param name="payload">Deleted task id and its column id.</param>
        Task TaskDeleted(TaskDeletedResponse payload);

        #endregion

        #region Task assignees (task:{id} group)

        /// <summary>A user was assigned to a task.</summary>
        /// <remarks>Source: <see cref="TaskInfoController"/> (<c>POST /api/tasks/{taskId}/user</c>) → task group. No payload — the client refetches the assignee list.</remarks>
        Task TaskUserAdded();

        /// <summary>A user was removed from a task.</summary>
        /// <remarks>Source: <see cref="TaskInfoController"/> (<c>DELETE /api/tasks/{taskId}/user</c>) → task group.</remarks>
        /// <param name="userId">Id of the removed user.</param>
        Task TaskUserRemoved(Guid userId);

        #endregion

        #region Task attachments (task:{id} group)

        /// <summary>A file attachment was uploaded to a task.</summary>
        /// <remarks>Source: <c>POST files/task/{taskId}/attachments</c> → task group. No payload — the client refetches the attachment list.</remarks>
        Task TaskAttachmentUploaded();

        /// <summary>A file attachment was deleted from a task.</summary>
        /// <remarks>Source: <c>DELETE files/task/{taskId}/attachments/{attachmentId}</c> → task group.</remarks>
        /// <param name="attachmentId">Id of the deleted attachment.</param>
        Task TaskAttachmentDeleted(Guid attachmentId);

        #endregion

        #region Task images (task:{id} group)

        /// <summary>An image was uploaded to a task.</summary>
        /// <remarks>Source: <c>POST files/task/{taskId}/images</c> → task group. No payload — the client refetches the image list.</remarks>
        Task TaskImageUploaded();

        /// <summary>An image was deleted from a task.</summary>
        /// <remarks>Source: <c>DELETE files/task/{taskId}/images/{imageId}</c> → task group.</remarks>
        /// <param name="imageId">Id of the deleted image.</param>
        Task TaskImageDeleted(Guid imageId);

        #endregion

        #region Presence (hub lifecycle, not REST)

        /// <summary>A user came online.</summary>
        /// <remarks>Source: <see cref="Controllers.Hubs.AppHub.OnConnectedAsync"/> → <c>Clients.Others</c>.</remarks>
        /// <param name="userId">Id of the user who connected.</param>
        Task UserConnected(Guid userId);

        /// <summary>A user went offline.</summary>
        /// <remarks>Source: <see cref="Controllers.Hubs.AppHub.OnDisconnectedAsync"/> → <c>Clients.All</c>.</remarks>
        /// <param name="userId">Id of the user who disconnected.</param>
        Task UserDisconnected(Guid userId);

        #endregion
    }
}
