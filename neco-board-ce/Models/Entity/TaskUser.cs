using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace neco_board_ce.Models.Entity
{
    [Table("task_users")]
    public class TaskUser
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("task_id")]
        public string TaskId { get; set; }

        [ForeignKey(nameof(TaskId))]
        [InverseProperty(nameof(ColumnTask.Users))]
        [JsonIgnore]
        public ColumnTask Task { get; set; }

        [Required]
        [Column("user_id")]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Account.Tasks))]
        [JsonIgnore]
        public Account User { get; set; }
    }
}