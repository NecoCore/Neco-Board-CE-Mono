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
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("project_id")]
        public string ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [JsonIgnore]
        public Project Project { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; }

        [Column("queue")]
        public int Queue { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public List<ColumnTask> Tasks { get; set; } = [];
    }
}