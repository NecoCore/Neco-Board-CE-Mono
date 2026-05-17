using Microsoft.EntityFrameworkCore;

namespace neco_board_ce.Data.Context
{
    public class SqliteDbContext : AppDbContext
    {
        public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options) { }
    }
}
