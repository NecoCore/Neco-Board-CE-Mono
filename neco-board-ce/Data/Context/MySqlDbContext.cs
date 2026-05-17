using Microsoft.EntityFrameworkCore;

namespace neco_board_ce.Data.Context
{
    public class MySqlDbContext : AppDbContext
    {
        public MySqlDbContext(DbContextOptions<MySqlDbContext> options) : base(options) { }
    }
}
