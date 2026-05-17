using Microsoft.EntityFrameworkCore;

namespace neco_board_ce.Data.Context
{
    public class PostgresDbContext : AppDbContext
    {
        public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }
    }
}
