using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class Album
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public required string Title { get; set; }

        public string? ArtistName { get; set; }

        public int ReleaseYear { get; set; }

        public int ArtistId { get; set; }

        public string? CoverImageUrl { get; set; }

        public Artist? Artist { get; set; }

        public List<Song>? Songs { get; set; }

        public List<FavoriteAlbum>? FavoriteAlbums { get; set; }
    }
}
