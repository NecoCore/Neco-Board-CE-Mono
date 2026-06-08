using neco_board_ce.Models.Enums;

namespace neco_board_ce.Interfaces
{
    public interface IRealtimeNotifier
    {
        #region Projects
        Task ProjectCreated();
        Task ProjectUpdated(string projectId, string projectName);
        Task ProjectDeleted(string projectId);
        #endregion

        #region Users in project
        Task ProjectAddUser(string projectId, string userId);
        Task ProjectUpdateUser(string projectId, string userId, ProjectRole newRole);
        Task ProjectRemoveUser(string projectId, string userId);
        #endregion

        #region Columns
        Task ColumnCreated(string projectId);
        Task ColumnUpdated(string projectId, string columnId, string columnName);
        Task ColumnOrderUpdated(string projectId);
        Task ColumnDelete(string projectId, string columnId);
        #endregion

        #region Tasks
        Task TaskCreated(string projectId, string columnId);
        Task TaskUpdated(string projectId, string taskId);
        Task TaskColumnUpdated(string projectId, string oldColumnId, string newColumnId);
        Task TaskStatusUpdated(string projectId, string taskId, string columnId, ColumnTaskStatus newStatus);
        Task TaskPriorityUpdated(string projectId, string taskId, string columnId, TaskPriority newPriority);
        Task TaskDelete(string projectId, string columnId, string taskId);
        #endregion

        #region Users in task
        Task TaskAddUser(string taskId);
        Task TaskRemoveUser(string taskId, string userId);
        #endregion

        #region Task attachments
        Task TaskAttachmentUploaded(string taskId);
        Task TaskAttachmentDeleted(string taskId, string attachmentId);
        #endregion

        #region Task images
        Task TaskImageUploaded(string taskId);
        Task TaskImageDeleted(string taskId, string imageId);
        #endregion
    }
}
