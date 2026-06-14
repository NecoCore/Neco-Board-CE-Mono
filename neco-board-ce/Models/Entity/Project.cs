using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace neco_board_ce.Models.Entity
{
    [Table("projects")]
    public class Project
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("is_archived")]
        public bool IsArchived { get; set; } = false;

        [Required]
        [Column("owner_id")]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey(nameof(OwnerId))]
        public Account Owner { get; set; } = null!;
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [JsonIgnore]
        public List<Logs> Logs { get; set; } = [];
        [JsonIgnore]
        public List<Column> Columns { get; set; } = [];
        [JsonIgnore]
        public List<UserProjectRole> UserProjectRoles { get; set; } = [];
    }
}
