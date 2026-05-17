using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request
{
    public class ColumnRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
