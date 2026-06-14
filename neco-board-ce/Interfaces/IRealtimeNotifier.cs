using neco_board_ce.Models.Enums;

namespace neco_board_ce.Interfaces
{
    public interface IRealtimeNotifier
    {
        #region Projects
        Task ProjectCreated();
        Task ProjectUpdated(Guid projectId, string projectName);
        Task ProjectDeleted(Guid projectId);
        #endregion

        #region Users in project
        Task ProjectAddUser(Guid projectId, Guid userId);
        Task ProjectUpdateUser(Guid projectId, Guid userId, ProjectRole newRole);
        Task ProjectRemoveUser(Guid projectId, Guid userId);
        #endregion

        #region Columns
        Task ColumnCreated(Guid projectId);
        Task ColumnUpdated(Guid projectId, Guid columnId, string columnName);
        Task ColumnOrderUpdated(Guid projectId);
        Task ColumnDelete(Guid projectId, Guid columnId);
        #endregion

        #region Tasks
        Task TaskCreated(Guid projectId, Guid columnId);
        Task TaskUpdated(Guid projectId, Guid taskId);
        Task TaskColumnUpdated(Guid projectId, Guid oldColumnId, Guid newColumnId);
        Task TaskStatusUpdated(Guid projectId, Guid taskId, Guid columnId, ColumnTaskStatus newStatus);
        Task TaskPriorityUpdated(Guid projectId, Guid taskId, Guid columnId, TaskPriority newPriority);
        Task TaskDelete(Guid projectId, Guid columnId, Guid taskId);
        #endregion

        #region Users in task
        Task TaskAddUser(Guid taskId);
        Task TaskRemoveUser(Guid taskId, Guid userId);
        #endregion

        #region Task attachments
        Task TaskAttachmentUploaded(Guid taskId);
        Task TaskAttachmentDeleted(Guid taskId, Guid attachmentId);
        #endregion

        #region Task images
        Task TaskImageUploaded(Guid taskId);
        Task TaskImageDeleted(Guid taskId, Guid imageId);
        #endregion
    }
}
