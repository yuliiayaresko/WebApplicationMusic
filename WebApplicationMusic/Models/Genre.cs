using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class Genre
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        public List<Artist>? Artists { get; set; }
    }
}
