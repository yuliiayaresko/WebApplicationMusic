using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [StringLength(100)]
        public required string Email { get; set; }

        public List<FavoriteAlbum>? FavoriteAlbums { get; set; }

        public List<Playlist>? Playlists { get; set; }
    }
}
