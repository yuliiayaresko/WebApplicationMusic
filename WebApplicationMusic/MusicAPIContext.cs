using Microsoft.EntityFrameworkCore;
using WebApplicationMusic.Models;

namespace WebApplicationMusic
{
    public class MusicAPIContext : DbContext
    {
        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<Artist> Artists { get; set; } = null!;
        public DbSet<Album> Albums { get; set; } = null!;
        public DbSet<Song> Songs { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<FavoriteAlbum> FavoriteAlbums { get; set; } = null!;
        public DbSet<Playlist> Playlists { get; set; } = null!;
        public DbSet<PlaylistSong> PlaylistSongs { get; set; } = null!;

        public MusicAPIContext(DbContextOptions<MusicAPIContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
