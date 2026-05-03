using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;

namespace WebApplicationMusic.Services
{
    public class AlbumService : IAlbumService
    {
        private readonly MusicAPIContext _context;
        private readonly IWebHostEnvironment _environment;

        public AlbumService(MusicAPIContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IEnumerable<Album>> GetAllAlbumsAsync()
        {
            return await _context.Albums
                .Include(a => a.Songs)
                .ToListAsync();
        }

        public async Task<Album?> GetAlbumByIdAsync(int id)
        {
            return await _context.Albums
                .Include(a => a.Songs)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Album>> SearchAlbumsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await _context.Albums.Include(a => a.Songs).ToListAsync();

            return await _context.Albums
                .Include(a => a.Songs)
                .Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            (a.ArtistName != null && a.ArtistName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            a.ReleaseYear.ToString().Contains(query))
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetFavoritesAsync(int userId)
        {
            return await _context.FavoriteAlbums
                .Where(f => f.UserId == userId)
                .Include(f => f.Album).ThenInclude(a => a.Artist)
                .Select(f => (object)new
                {
                    Id = f.Id,
                    AlbumId = f.Album.Id,
                    Rating = f.Rating,
                    Title = f.Album.Title,
                    ReleaseYear = f.Album.ReleaseYear,
                    CoverImageUrl = f.Album.CoverImageUrl,
                    ArtistName = f.Album.Artist != null ? f.Album.Artist.Name : f.Album.ArtistName
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetFavoritesByUserAsync(int userId)
        {
            return await _context.FavoriteAlbums
                .Where(f => f.UserId == userId)
                .Include(f => f.Album).ThenInclude(a => a.Artist)
                .Select(f => (object)new
                {
                    Id = f.Album.Id,
                    AlbumId = f.AlbumId,
                    Rating = f.Rating,
                    Title = f.Album.Title,
                    ReleaseYear = f.Album.ReleaseYear,
                    CoverImageUrl = f.Album.CoverImageUrl,
                    ArtistName = f.Album.Artist != null ? f.Album.Artist.Name : f.Album.ArtistName
                })
                .ToListAsync();
        }

        public async Task<Album> CreateAlbumAsync(AlbumDto dto, IFormFile? coverImage)
        {
            if (coverImage != null)
            {
                if (coverImage.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("Зображення занадто велике. Максимальний розмір: 5 МБ.");

                if (!coverImage.ContentType.StartsWith("image/"))
                    throw new ArgumentException("Дозволені лише зображення.");

                var fileName = $"album-{Guid.NewGuid()}{Path.GetExtension(coverImage.FileName)}";
                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
                Directory.CreateDirectory(Path.Combine(_environment.WebRootPath, "images"));
                using var stream = new FileStream(filePath, FileMode.Create);
                await coverImage.CopyToAsync(stream);
                dto.CoverImageUrl = $"/images/{fileName}";
            }

            var album = new Album
            {
                Title = dto.Title,
                ReleaseYear = dto.ReleaseYear,
                ArtistId = dto.ArtistId,
                ArtistName = dto.ArtistName,
                CoverImageUrl = dto.CoverImageUrl
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            return album;
        }

        public async Task<bool> UpdateAlbumAsync(int id, AlbumDto dto, IFormFile? coverImage)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return false;

            album.Title = dto.Title;
            album.ReleaseYear = dto.ReleaseYear;
            album.ArtistId = dto.ArtistId;
            album.ArtistName = dto.ArtistName;

            if (coverImage != null)
            {
                if (coverImage.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("Зображення занадто велике. Максимальний розмір: 5 МБ.");

                if (!coverImage.ContentType.StartsWith("image/"))
                    throw new ArgumentException("Дозволені лише зображення.");

                var fileName = $"album-{Guid.NewGuid()}{Path.GetExtension(coverImage.FileName)}";
                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
                Directory.CreateDirectory(Path.Combine(_environment.WebRootPath, "images"));
                using var stream = new FileStream(filePath, FileMode.Create);
                await coverImage.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(album.CoverImageUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, album.CoverImageUrl.TrimStart('/'));
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }

                album.CoverImageUrl = $"/images/{fileName}";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAlbumAsync(int id)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return false;

            if (!string.IsNullOrEmpty(album.CoverImageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, album.CoverImageUrl.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FavoriteAlbum?> AddToFavoritesAsync(int albumId, int userId)
        {
            var album = await _context.Albums.FindAsync(albumId);
            if (album == null) return null;

            var existing = await _context.FavoriteAlbums
                .FirstOrDefaultAsync(f => f.AlbumId == albumId && f.UserId == userId);
            if (existing != null)
                throw new InvalidOperationException("Альбом уже в улюблених.");

            var favorite = new FavoriteAlbum { AlbumId = albumId, UserId = userId, Rating = 0 };
            _context.FavoriteAlbums.Add(favorite);
            await _context.SaveChangesAsync();
            return favorite;
        }

        public async Task<bool> RemoveFromFavoritesAsync(int albumId, int userId)
        {
            var favorite = await _context.FavoriteAlbums
                .FirstOrDefaultAsync(f => f.AlbumId == albumId && f.UserId == userId);
            if (favorite == null) return false;

            _context.FavoriteAlbums.Remove(favorite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FavoriteAlbum?> RateAlbumAsync(int favoriteId, int rating)
        {
            var favorite = await _context.FavoriteAlbums.FindAsync(favoriteId);
            if (favorite == null) return null;

            favorite.Rating = rating;
            await _context.SaveChangesAsync();
            return favorite;
        }
    }
}