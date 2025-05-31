using System.ComponentModel.DataAnnotations;

namespace WebApplicationMusic.Models
{
    public class PlaylistSong
    {
        [Key]
        public int Id { get; set; }

        public int PlaylistId { get; set; }
        public Playlist? Playlist { get; set; }

        public int SongId { get; set; }
        public Song? Song { get; set; }
    }
}
