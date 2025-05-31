using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class Artist
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int? GenreId { get; set; }
        public Genre? Genre { get; set; }

        public List<Album>? Albums { get; set; }
    }
}
