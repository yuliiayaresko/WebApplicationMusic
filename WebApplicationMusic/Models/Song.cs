using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class Song
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [StringLength(5)]
        public string? Duration { get; set; }

        public int? AlbumId { get; set; }

        [StringLength(100)]
        public string? Artist { get; set; }

        [StringLength(255)]
        public string? AudioUrl { get; set; }

        public Album? Album { get; set; }

        public List<PlaylistSong>? PlaylistSongs { get; set; }
    }
}
