namespace neco_board_ce.Data
{
    public static class Constants
    {
        #region Roles
        public static readonly string ROLE_ADMIN = "ADMIN";
        public static readonly string ROLE_OWNER = "OWNER";
        #endregion

        #region Groups
        public static readonly string GROUP_ADMINS = "AdminsGroup";
        public static readonly string GROUP_ALL = "all";
        #endregion

        #region Soket events
        #region Projects
        public static readonly string SOKET_EVENT_PROJECT_CREATED = "ProjectCreated";
        public static readonly string SOKET_EVENT_PROJECT_UPDATED = "ProjectUpdated";
        public static readonly string SOKET_EVENT_PROJECT_DELETED = "ProjectDeleted";
        #endregion

        #region Projects columns
        public static readonly string SOKET_EVENT_COLUMN_CREATED = "ColumnCreated";
        public static readonly string SOKET_EVENT_COLUMN_UPDATED = "ColumnUpdated";
        public static readonly string SOKET_EVENT_COLUMN_UPDATED_ORDER = "ColumnUpdatedOrder";
        public static readonly string SOKET_EVENT_COLUMN_DELETED = "ColumnDeleted";
        #endregion

        #region Tasks in columns
        public static readonly string SOKET_EVENT_TASK_CREATED = "TaskCreated";
        public static readonly string SOKET_EVENT_TASK_UPDATED = "TaskUpdated";
        public static readonly string SOKET_EVENT_TASK_PRIORITY_UPDATED = "TaskPriorityUpdated";
        public static readonly string SOKET_EVENT_TASK_STATUS_UPDATED = "TaskStatusUpdated";
        public static readonly string SOKET_EVENT_TASK_COLUMN_UPDATED = "TaskColumnUpdated";
        public static readonly string SOKET_EVENT_TASK_DELETED = "TaskDeleted";
        #endregion

        #region Task
        public static readonly string SOKET_EVENT_TASK_USER_ADDED = "TaskUserAdded";
        public static readonly string SOKET_EVENT_TASK_USER_REMOVED = "TaskUserRemoved";
        #endregion

        #region Usera in project
        public static readonly string SOKET_EVENT_USER_ADDED_TO_PROJECT = "UserAddedToProject";
        public static readonly string SOKET_EVENT_USER_REMOVED_FROM_PROJECT = "UserRemovedFromProject";
        public static readonly string SOKET_EVENT_USER_ROLE_UPDATED_IN_PROJECT = "UserRoleUpdatedInProject";
        #endregion

        #region User online
        public static readonly string SOKET_EVENT_USER_CONNECT = "UserConnected";
        public static readonly string SOKET_EVENT_USER_DISCONNECT = "UserDisconnected";
        #endregion
        #endregion
    }
}
