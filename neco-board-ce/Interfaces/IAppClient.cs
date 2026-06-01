using neco_board_ce.Controllers.API;
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
    /// the column id used to pick a group) and forwards the rest here. Each member documents its
    /// source REST route (or hub lifecycle method) and the target SignalR group(s).
    /// <para>
    /// Enum payloads (<see cref="ColumnTaskStatus"/>, <see cref="TaskPriority"/>,
    /// <see cref="ProjectRole"/>) are serialized as strings because SignalR is configured with
    /// a <c>JsonStringEnumConverter</c> via <c>AddJsonProtocol</c>.
    /// </para>
    /// <para>
    /// Note: these XML comments feed IntelliSense / DocFX only. They are NOT consumed by
    /// Saunter / AsyncAPI — that tooling reads its own attributes instead.
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
        /// <param name="id">Id of the updated project.</param>
        /// <param name="name">New project name (lets the client patch in place without a refetch).</param>
        Task ProjectUpdated(string id, string name);

        /// <summary>A project was deleted.</summary>
        /// <remarks>
        /// Source: <see cref="ProjectController"/> (<c>DELETE /api/project/{id}</c>) →
        /// project group <c>project:{id}</c> and the <c>all</c> group.
        /// </remarks>
        /// <param name="id">Id of the deleted project.</param>
        Task ProjectDeleted(string id);

        #endregion

        #region Project membership (project:{id} group + the affected user)

        /// <summary>A user was added to a project.</summary>
        /// <remarks>Source: <see cref="UserProjectController"/> (<c>POST /api/project/{projectId}/users</c>) → project group.</remarks>
        /// <param name="userId">Id of the added user.</param>
        Task UserAddedToProject(string userId);

        /// <summary>A user's role in a project changed.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>PATCH /api/project/{projectId}/users/{userId}</c>)
        /// → project group and the affected user.
        /// </remarks>
        /// <param name="userId">Id of the affected user.</param>
        /// <param name="role">The new project role (lets the client update the member entry in place).</param>
        Task UserRoleUpdatedInProject(string userId, ProjectRole role);

        /// <summary>A user was removed from a project.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>DELETE /api/project/{projectId}/users/{userId}</c>)
        /// → project group and the affected user.
        /// </remarks>
        /// <param name="userId">Id of the removed user.</param>
        Task UserRemovedFromProject(string userId);

        #endregion

        #region Columns (always to the project:{id} group)

        /// <summary>A column was created.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>POST /api/column/in-project/{projectId}</c>). No payload — the client refetches the column list.</remarks>
        Task ColumnCreated();

        /// <summary>A column was updated (renamed).</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>PUT /api/column/{columnId}</c>).</remarks>
        /// <param name="columnId">Id of the updated column.</param>
        /// <param name="name">New column name.</param>
        Task ColumnUpdated(string columnId, string name);

        /// <summary>Column order changed.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>PUT /api/column/{columnId}/order</c>). No payload — the client refetches the column order.</remarks>
        Task ColumnUpdatedOrder();

        /// <summary>A column was deleted.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>DELETE /api/column/{columnId}</c>).</remarks>
        /// <param name="columnId">Id of the deleted column.</param>
        Task ColumnDeleted(string columnId);

        #endregion

        #region Tasks (board level, project:{id} group)

        /// <summary>A task was created.</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>POST /api/tasks</c>).</remarks>
        /// <param name="columnId">Id of the column the task was created in.</param>
        Task TaskCreated(string columnId);

        /// <summary>A task was updated (content fields).</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>PUT /api/tasks/{taskId}</c>).</remarks>
        /// <param name="id">Id of the updated task.</param>
        Task TaskUpdated(string id);

        /// <summary>A task was moved between columns.</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>PATCH /api/tasks/{taskId}/column</c>) → project group.</remarks>
        /// <param name="oldColumn">Id of the source column.</param>
        /// <param name="newColumn">Id of the destination column.</param>
        Task TaskColumnUpdated(string oldColumn, string newColumn);

        /// <summary>Task status changed.</summary>
        /// <remarks>
        /// Source: <see cref="TaskInfoController"/> (<c>PATCH /api/tasks/{taskId}/status</c>) →
        /// task group <c>task:{taskId}</c> and project group <c>project:{id}</c>.
        /// </remarks>
        /// <param name="taskId">Id of the task.</param>
        /// <param name="columnId">Id of the task's column (lets the board locate the card).</param>
        /// <param name="status">The new status.</param>
        Task TaskStatusUpdated(string taskId, string columnId, ColumnTaskStatus status);

        /// <summary>Task priority changed.</summary>
        /// <remarks>
        /// Source: <see cref="TaskInfoController"/> (<c>PATCH /api/tasks/{taskId}/priority</c>) →
        /// task group and project group.
        /// </remarks>
        /// <param name="taskId">Id of the task.</param>
        /// <param name="columnId">Id of the task's column (lets the board locate the card).</param>
        /// <param name="priority">The new priority.</param>
        Task TaskPriorityUpdated(string taskId, string columnId, TaskPriority priority);

        /// <summary>A task was deleted.</summary>
        /// <remarks>
        /// Source: <see cref="TaskColumnController"/> (<c>DELETE /api/tasks/{taskId}</c>) →
        /// task group <c>task:{taskId}</c> and project group <c>project:{id}</c>.
        /// </remarks>
        /// <param name="taskId">Id of the deleted task.</param>
        /// <param name="columnId">Id of the column the task belonged to.</param>
        Task TaskDeleted(string taskId, string columnId);

        #endregion

        #region Task assignees (task:{id} group)

        /// <summary>A user was assigned to a task.</summary>
        /// <remarks>Source: <see cref="TaskInfoController"/> (<c>POST /api/tasks/{taskId}/user</c>) → task group. No payload — the client refetches the assignee list.</remarks>
        Task TaskUserAdded();

        /// <summary>A user was removed from a task.</summary>
        /// <remarks>Source: <see cref="TaskInfoController"/> (<c>DELETE /api/tasks/{taskId}/user</c>) → task group.</remarks>
        /// <param name="userId">Id of the removed user.</param>
        Task TaskUserRemoved(string userId);

        #endregion

        #region Presence (hub lifecycle, not REST)

        /// <summary>A user came online.</summary>
        /// <remarks>Source: <see cref="Controllers.Hubs.AppHub.OnConnectedAsync"/> → <c>Clients.Others</c>.</remarks>
        /// <param name="userId">Id of the user who connected.</param>
        Task UserConnected(string userId);

        /// <summary>A user went offline.</summary>
        /// <remarks>Source: <see cref="Controllers.Hubs.AppHub.OnDisconnectedAsync"/> → <c>Clients.All</c>.</remarks>
        /// <param name="userId">Id of the user who disconnected.</param>
        Task UserDisconnected(string userId);

        #endregion
    }
}
