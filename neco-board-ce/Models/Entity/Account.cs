using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.Entity
{
    [Table("accounts")]
    public class Account
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(50)]
        [Column("name")]
        public string Name { get; set; }
        
        [MaxLength(500)]
        [Column("avatar")]
        public string? Avatar { get; set; }
        
        [Required]
        [MaxLength(50)]
        [Column("login")]
        [JsonIgnore]
        public string Login { get; set; }
        
        [Required]
        [MaxLength(500)]
        [Column("password")]
        [JsonIgnore]
        public string Password { get; set; }
        
        [Column("role")]
        public WorkspaceRoles Role { get; set; } = WorkspaceRoles.USER;
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        
        [JsonIgnore]
        public List<Logs> Logs { get; set; } = [];
        [JsonIgnore]
        public List<UserProjectRole> Projects { get; set; } = [];
        [JsonIgnore]
        public List<TaskUser> Tasks { get; set; } = [];
        [JsonIgnore]
        public List<ColumnTask> OwnerTasks { get; set; } = [];
        [JsonIgnore]
        public List<RefreshTokens> RefreshTokens { get; set; } = [];
    }
}
