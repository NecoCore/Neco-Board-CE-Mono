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
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("user_id")]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Account.Projects))]
        [JsonIgnore]
        public Account User { get; set; }

        [Required]
        [Column("project_id")]
        public string ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [InverseProperty(nameof(Project.UserProjectRoles))]
        [JsonIgnore]
        public Project Project { get; set; }

        [Column("role")]
        public ProjectRole Role { get; set; } = ProjectRole.USER;
    }
}