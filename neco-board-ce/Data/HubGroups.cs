namespace neco_board_ce.Data
{
    public static class HubGroups
    {
        public static string Project(string id) => $"project:{id}";
        public static string Task(string id) => $"task:{id}";
        public const string All = "all";
        public const string Admins = "AdminsGroup";
    }
}
