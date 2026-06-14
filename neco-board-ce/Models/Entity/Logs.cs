using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.Entity
{
    [Table("logs")]
    public class Logs
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Account.Logs))]
        [JsonIgnore]
        public Account User { get; set; } = null!;

        [Column("project_id")]
        public Guid? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [InverseProperty(nameof(Project.Logs))]
        [JsonIgnore]
        public Project? Project { get; set; }

        [Column("new_user_id")]
        public Guid? NewUserId { get; set; }

        [ForeignKey(nameof(NewUserId))]
        [JsonIgnore]
        public Account? NewUser { get; set; }

        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("log_type")]
        public LogType LogType { get; set; }

        [Column("log_for")]
        public LogFor LogFor { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}