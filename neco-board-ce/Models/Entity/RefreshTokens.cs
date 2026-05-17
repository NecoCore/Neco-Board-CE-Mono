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
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("token")]
        public string Token { get; set; }

        [Required]
        public string AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        [JsonIgnore]
        public Account Account { get; set; }

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
