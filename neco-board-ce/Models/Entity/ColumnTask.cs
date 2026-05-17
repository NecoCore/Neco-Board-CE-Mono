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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [Column("column_id")]
        public string ColumnId { get; set; }
        
        [ForeignKey(nameof(ColumnId))]
        [JsonIgnore]
        public Column Column { get; set; }
        
        [Required]
        [Column("owner_id")]
        public string OwnerId { get; set; }
        
        [ForeignKey(nameof(OwnerId))]
        [InverseProperty(nameof(Account.OwnerTasks))]
        public Account Owner { get; set; }
        
        [Required]
        [MaxLength(250)]
        [Column("name")]
        public string Name { get; set; }
        
        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }
        
        [Column("text", TypeName = "text")]
        public string Text { get; set; }

        [Column("priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.LOW;

        [Column("status")]
        public ColumnTaskStatus Status { get; set; } = ColumnTaskStatus.NOT_STARTED;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;


        [JsonIgnore]
        public List<TaskImages> Images { get; set; } = [];
        [JsonIgnore]
        public List<TaskUser> Users { get; set; } = [];
        [JsonIgnore]
        public List<TaskAttachments> Attachments { get; set; } = [];
    }
}
