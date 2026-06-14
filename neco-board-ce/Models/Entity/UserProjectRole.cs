using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.Entity
{
    [Table("user_project_roles")]
    public class UserProjectRole
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Account.Projects))]
        [JsonIgnore]
        public Account User { get; set; } = null!;

        [Required]
        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [InverseProperty(nameof(Project.UserProjectRoles))]
        [JsonIgnore]
        public Project Project { get; set; } = null!;

        [Column("role")]
        public ProjectRole Role { get; set; } = ProjectRole.USER;
    }
}