using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace neco_board_ce.Models.Entity
{
    [Table("task_attachments")]
    public class TaskAttachments
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("task_id")]
        public string TaskId { get; set; }

        [ForeignKey(nameof(TaskId))]
        [InverseProperty(nameof(ColumnTask.Attachments))]
        [JsonIgnore]
        public ColumnTask Task { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("type")]
        public string Type { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("file_path")]
        public string FilePath { get; set; }
    }
}