using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace neco_board_ce.Models.Entity
{
    [Table("columns")]
    public class Column
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [JsonIgnore]
        public Project Project { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("queue")]
        public int Queue { get; set; }

        [Column("color")]
        public string? Color { get; set; } = "#FFFFFF";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public List<ColumnTask> Tasks { get; set; } = [];
    }
}