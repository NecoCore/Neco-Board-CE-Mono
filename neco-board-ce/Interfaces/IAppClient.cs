using neco_board_ce.Controllers.API;
using neco_board_ce.Models.DTO.Response.Projects;

namespace neco_board_ce.Interfaces
{
    /// <summary>
    /// Strongly-typed contract of all real-time events the server pushes to clients over SignalR.
    /// </summary>
    /// <remarks>
    /// The method name equals the event name on the client; the parameter is the payload.
    /// Routing (which group / which user receives the event) is NOT part of this contract —
    /// it lives in the notifier. Each member documents its source REST route (or hub lifecycle
    /// method) and the target SignalR group(s).
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
        /// → the added user (<c>User(userId)</c>).
        /// </remarks>
        /// <param name="project">The created project.</param>
        Task ProjectCreated(ProjectItemResponse project);

        /// <summary>A project was updated.</summary>
        /// <remarks>
        /// Source: <see cref="ProjectController"/> (<c>PUT /api/project/{id}</c>) →
        /// project group <c>project:{id}</c> and the admins group.
        /// Unified: previously the project group received no payload.
        /// </remarks>
        /// <param name="id">Id of the updated project.</param>
        Task ProjectUpdated(string id);

        /// <summary>A project was deleted.</summary>
        /// <remarks>
        /// Source: <see cref="ProjectController"/> (<c>DELETE /api/project/{id}</c>) →
        /// project group <c>project:{id}</c> and the <c>all</c> group.
        /// Unified: previously the project group received no payload.
        /// </remarks>
        /// <param name="id">Id of the deleted project.</param>
        Task ProjectDeleted(string id);

        #endregion

        #region Columns (always to the project:{id} group)

        /// <summary>A column was created.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>POST /api/column/in-project/{projectId}</c>). No payload.</remarks>
        Task ColumnCreated();

        /// <summary>A column was updated.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>PUT /api/column/{columnId}</c>). No payload.</remarks>
        Task ColumnUpdated();

        /// <summary>Column order changed.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>PUT /api/column/{columnId}/order</c>). No payload.</remarks>
        Task ColumnUpdatedOrder();

        /// <summary>A column was deleted.</summary>
        /// <remarks>Source: <see cref="ColumsProjectController"/> (<c>DELETE /api/column/{columnId}</c>). No payload.</remarks>
        Task ColumnDeleted();

        #endregion

        #region Tasks (board level, project:{id} group)

        /// <summary>A task was created.</summary>
        /// <remarks>
        /// Source: <see cref="TaskColumnController"/> (<c>POST /api/tasks</c>).
        /// Note: the controller currently sends the column id (string) as payload, not a project DTO.
        /// </remarks>
        /// <param name="project">Payload (review: controller emits the column id).</param>
        Task TaskCreated(ProjectItemResponse project);

        /// <summary>A task was updated.</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>PUT /api/tasks/{taskId}</c>).</remarks>
        /// <param name="id">Id of the updated task.</param>
        Task TaskUpdated(string id);

        /// <summary>Task status changed.</summary>
        /// <remarks>
        /// Source: <see cref="TaskInfoController"/> (<c>PATCH /api/tasks/{taskId}/status</c>) →
        /// task group <c>task:{taskId}</c> and project group <c>project:{id}</c>.
        /// Note: the controller currently emits no payload to the task group and the column id
        /// to the project group — pick one consistent payload for both.
        /// </remarks>
        /// <param name="id">Task id (or column id — see note).</param>
        Task TaskStatusUpdated(string id);

        /// <summary>Task priority changed.</summary>
        /// <remarks>
        /// Source: <see cref="TaskInfoController"/> (<c>PATCH /api/tasks/{taskId}/priority</c>) →
        /// task group and project group.
        /// Note: this event is currently emitted with three different payload shapes across the
        /// codebase (none / EditTaskSocketResponse / task id from DeleteTask) — unify them.
        /// </remarks>
        /// <param name="id">Task id.</param>
        Task TaskPriorityUpdated(string id);

        /// <summary>A task was moved between columns.</summary>
        /// <remarks>Source: <see cref="TaskColumnController"/> (<c>PATCH /api/tasks/{taskId}/column</c>) → project group.</remarks>
        /// <param name="oldColumn">Id of the source column.</param>
        /// <param name="newColumn">Id of the destination column.</param>
        Task TaskColumnUpdated(string oldColumn, string newColumn);

        /// <summary>A task was deleted.</summary>
        /// <remarks>
        /// Source: <see cref="TaskColumnController"/> (<c>DELETE /api/tasks/{taskId}</c>) →
        /// task group <c>task:{taskId}</c>.
        /// Recommendation: send this same event to the project group too, instead of the current
        /// workaround that reuses <see cref="TaskPriorityUpdated"/>.
        /// </remarks>
        /// <param name="id">Id of the deleted task.</param>
        Task TaskDeleted(string id);

        #endregion

        #region Task assignees (task:{id} group)

        /// <summary>A user was assigned to a task.</summary>
        /// <remarks>Source: <see cref="TaskInfoController"/> (<c>POST /api/tasks/{taskId}/user</c>) → task group. No payload.</remarks>
        Task TaskUserAdded();

        /// <summary>A user was removed from a task.</summary>
        /// <remarks>Source: <see cref="TaskInfoController"/> (<c>DELETE /api/tasks/{taskId}/user</c>) → task group. No payload.</remarks>
        Task TaskUserRemoved();

        #endregion

        #region Project membership

        /// <summary>A user was added to a project.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>POST /api/project/{projectId}/users</c>) → project group.
        /// (Previously sent without payload; <paramref name="userId"/> added.)
        /// </remarks>
        /// <param name="userId">Id of the added user.</param>
        Task UserAddedToProject(string userId);

        /// <summary>A user's role in a project changed.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>PATCH /api/project/{projectId}/users/{userId}</c>)
        /// → project group and the affected user.
        /// </remarks>
        /// <param name="userId">Id of the affected user.</param>
        Task UserRoleUpdatedInProject(string userId);

        /// <summary>A user was removed from a project.</summary>
        /// <remarks>
        /// Source: <see cref="UserProjectController"/> (<c>DELETE /api/project/{projectId}/users/{userId}</c>)
        /// → project group and the affected user.
        /// </remarks>
        /// <param name="userId">Id of the removed user.</param>
        Task UserRemovedFromProject(string userId);

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
