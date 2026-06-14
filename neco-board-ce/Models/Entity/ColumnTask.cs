using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.Entity
{
    [Table("column_tasks")]
    public class ColumnTask
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("column_id")]
        public Guid ColumnId { get; set; }

        [ForeignKey(nameof(ColumnId))]
        [JsonIgnore]
        public Column Column { get; set; } = null!;
        
        [Required]
        [Column("owner_id")]
        public Guid OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        [InverseProperty(nameof(Account.OwnerTasks))]
        public Account Owner { get; set; } = null!;
        
        [Required]
        [MaxLength(250)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }
        
        [Column("text", TypeName = "text")]
        public string Text { get; set; } = string.Empty;

        [Column("priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.LOW;

        [Column("status")]
        public ColumnTaskStatus Status { get; set; } = ColumnTaskStatus.NOT_STARTED;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [JsonIgnore]
        public List<TaskImages> Images { get; set; } = [];
        [JsonIgnore]
        public List<TaskUser> Users { get; set; } = [];
        [JsonIgnore]
        public List<TaskAttachments> Attachments { get; set; } = [];
    }
}
