using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace neco_board_ce.Models.Entity
{
    [Table("tokens")]
    public class RefreshTokens
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string AccountId { get; set; } = string.Empty;

        [ForeignKey(nameof(AccountId))]
        [JsonIgnore]
        public Account Account { get; set; } = null!;

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
