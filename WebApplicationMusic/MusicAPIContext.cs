using Microsoft.EntityFrameworkCore;
using WebApplicationMusic.Models;
namespace WebApplicationMusic

{
    public class MusicAPIContext : DbContext
    {
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<FavoriteAlbum> FavoriteAlbums { get; set; }

        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public MusicAPIContext(DbContextOptions<MusicAPIContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

    }
}
