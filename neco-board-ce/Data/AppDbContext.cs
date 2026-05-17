using Microsoft.EntityFrameworkCore;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<ColumnTask> ColumnTasks { get; set; }
        public DbSet<UserProjectRole> UserProjectRoles { get; set; }
        public DbSet<TaskUser> TaskUsers { get; set; }
        public DbSet<TaskImages> TaskImages { get; set; }
        public DbSet<TaskAttachments> TaskAttachments { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<RefreshTokens> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // enums
            modelBuilder.Entity<Account>()
                .Property(a => a.Role)
                .HasConversion<string>();

            modelBuilder.Entity<UserProjectRole>()
                .Property(r => r.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Logs>()
                .Property(l => l.LogType)
                .HasConversion<string>();

            modelBuilder.Entity<ColumnTask>()
                .Property(t => t.Priority)
                .HasConversion<string>();

            modelBuilder.Entity<ColumnTask>()
                .Property(t => t.Status)
                .HasConversion<string>();

            // relations
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ColumnTask>()
                .HasOne(t => t.Owner)
                .WithMany(a => a.OwnerTasks)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Logs>()
                .HasOne(l => l.User)
                .WithMany(a => a.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshTokens>()
                .HasOne(r => r.Account)
                .WithMany(a => a.RefreshTokens)
                .HasForeignKey(r => r.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // unique constraints
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Login)
                .IsUnique();
        }
    }
}