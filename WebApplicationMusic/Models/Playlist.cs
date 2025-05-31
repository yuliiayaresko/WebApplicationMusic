using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class Playlist
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public List<PlaylistSong>? PlaylistSongs { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
