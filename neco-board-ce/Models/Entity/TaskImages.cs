using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace neco_board_ce.Models.Entity
{
    [Table("task_images")]
    public class TaskImages
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("task_id")]
        public Guid TaskId { get; set; }

        [ForeignKey(nameof(TaskId))]
        [InverseProperty(nameof(ColumnTask.Images))]
        [JsonIgnore]
        public ColumnTask Task { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("image_path")]
        public string ImagePath { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}