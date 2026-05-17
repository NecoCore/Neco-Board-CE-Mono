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
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("user_id")]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Account.Logs))]
        [JsonIgnore]
        public Account User { get; set; }

        [Required]
        [Column("project_id")]
        public string ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [InverseProperty(nameof(Project.Logs))]
        [JsonIgnore]
        public Project Project { get; set; }

        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("log_type")]
        public LogType LogType { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}