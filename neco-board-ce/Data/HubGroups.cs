namespace neco_board_ce.Data
{
    public static class HubGroups
    {
        public static string Project(string id) => $"project:{id}";
        public static string Task(string id) => $"task:{id}";
        public static string All = "all";
        public static string Admins = "AdminsGroup";
    }
}
