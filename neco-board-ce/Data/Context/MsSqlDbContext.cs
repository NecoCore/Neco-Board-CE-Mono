using Microsoft.EntityFrameworkCore;

namespace neco_board_ce.Data.Context
{
    public class MsSqlDbContext : AppDbContext
    {
        public MsSqlDbContext(DbContextOptions<MsSqlDbContext> options) : base(options) { }
    }
}
