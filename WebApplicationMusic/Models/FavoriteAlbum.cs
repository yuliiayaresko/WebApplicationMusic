using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class FavoriteAlbum
    {
        [Key]
        public int Id { get; set; }
        public string? ArtistName { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; } = null;
        public int? Rating { get; set; }

        public int AlbumId { get; set; }
        public Album? Album { get; set; }
    }
}
