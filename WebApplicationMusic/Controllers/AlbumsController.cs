using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationMusic;
using WebApplicationMusic.Models;
using WebApplicationMusic.Services;

namespace WebApplicationMusic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly MusicAPIContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAlbumService? _albumService;

        // Старий конструктор — для старих тестів
        public AlbumsController(MusicAPIContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Новий конструктор — для Moq тестів
        public AlbumsController(IAlbumService albumService)
        {
            _albumService = albumService;
        }

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Album>>> GetAlbums()
        {
            if (_albumService != null)
            {
                var result = await _albumService.GetAllAlbumsAsync();
                return Ok(result);
            }
            return await _context.Albums
                .Include(a => a.Songs)
                .ToListAsync();
        }

        // GET: api/Albums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Album>> GetAlbum(int id)
        {
            if (_albumService != null)
            {
                var result = await _albumService.GetAlbumByIdAsync(id);
                if (result == null) return NotFound();
                return Ok(result);
            }

            var album = await _context.Albums
                .Include(a => a.Songs)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
            {
                return NotFound();
            }

            return album;
        }

        // GET: api/Albums/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Album>>> SearchAlbums(string query)
        {
            if (_albumService != null)
            {
                var result = await _albumService.SearchAlbumsAsync(query);
                return Ok(result);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.Albums
                    .Include(a => a.Songs)
                    .ToListAsync();
            }

            query = query.ToLower();
            var albums = await _context.Albums
                .Include(a => a.Songs)
                .Where(a => a.Title.ToLower().Contains(query) ||
                            a.ArtistName.ToLower().Contains(query) ||
                            a.ReleaseYear.ToString().Contains(query))
                .ToListAsync();

            return albums;
        }

        // GET: api/Albums/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavorites([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("UserId є обов'язковим і має бути більше 0.");
            }

            if (_albumService != null)
            {
                var result = await _albumService.GetFavoritesAsync(userId);
                return Ok(result);
            }

            var favorites = await _context.FavoriteAlbums
                .Where(f => f.UserId == userId)
                .Include(f => f.Album)
                    .ThenInclude(a => a.Artist)
                .Select(f => new
                {
                    Id = f.Id,
                    AlbumId = f.Album.Id,
                    Rating = f.Rating,
                    Title = f.Album.Title,
                    ReleaseYear = f.Album.ReleaseYear,
                    CoverImageUrl = f.Album.CoverImageUrl,
                    ArtistName = f.Album.Artist != null ? f.Album.Artist.Name : f.Album.ArtistName,
                    Songs = f.Album.Songs.Select(s => new { s.Title, s.Duration })
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // GET: api/FavoriteAlbums/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavoriteAlbumsByUser(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("UserId є обов'язковим і має бути більше 0.");
            }

            if (_albumService != null)
            {
                var result = await _albumService.GetFavoritesByUserAsync(userId);
                return Ok(result);
            }

            var favorites = await _context.FavoriteAlbums
                .Where(f => f.UserId == userId)
                .Include(f => f.Album)
                    .ThenInclude(a => a.Artist)
                .Select(f => new
                {
                    Id = f.Album.Id,
                    AlbumId = f.AlbumId,
                    Rating = f.Rating,
                    Title = f.Album.Title,
                    ReleaseYear = f.Album.ReleaseYear,
                    CoverImageUrl = f.Album.CoverImageUrl,
                    ArtistName = f.Album.Artist != null ? f.Album.Artist.Name : f.Album.ArtistName,
                    Songs = f.Album.Songs.Select(s => new { s.Title, s.Duration })
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // POST: api/Albums
        [HttpPost]
        public async Task<ActionResult<Album>> PostAlbum([FromForm] AlbumDto albumDto, IFormFile? coverImage)
        {
            if (_albumService != null)
            {
                try
                {
                    var result = await _albumService.CreateAlbumAsync(albumDto, coverImage);
                    return CreatedAtAction("GetAlbum", new { id = result.Id }, result);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            if (coverImage != null)
            {
                if (coverImage.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("Зображення занадто велике. Максимальний розмір: 5 МБ.");
                }
                if (!coverImage.ContentType.StartsWith("image/"))
                {
                    return BadRequest("Дозволені лише зображення.");
                }

                var fileName = $"album-{Guid.NewGuid()}{Path.GetExtension(coverImage.FileName)}";
                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
                Directory.CreateDirectory(Path.Combine(_environment.WebRootPath, "images"));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(stream);
                }
                albumDto.CoverImageUrl = $"/images/{fileName}";
            }

            var album = new Album
            {
                Title = albumDto.Title,
                ReleaseYear = albumDto.ReleaseYear,
                ArtistId = albumDto.ArtistId,
                ArtistName = albumDto.ArtistName,
                CoverImageUrl = albumDto.CoverImageUrl
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAlbum", new { id = album.Id }, album);
        }

        // PUT: api/Albums/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlbum(int id, [FromForm] AlbumDto albumDto, IFormFile? coverImage)
        {
            if (id != albumDto.Id)
            {
                return BadRequest("Id у маршруті не збігається з Id у тілі запиту.");
            }

            if (_albumService != null)
            {
                try
                {
                    var updated = await _albumService.UpdateAlbumAsync(id, albumDto, coverImage);
                    if (!updated) return NotFound();
                    return NoContent();
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            var existingAlbum = await _context.Albums.FindAsync(id);
            if (existingAlbum == null)
            {
                return NotFound();
            }

            existingAlbum.Title = albumDto.Title;
            existingAlbum.ReleaseYear = albumDto.ReleaseYear;
            existingAlbum.ArtistId = albumDto.ArtistId;
            existingAlbum.ArtistName = albumDto.ArtistName;

            if (coverImage != null)
            {
                if (coverImage.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("Зображення занадто велике. Максимальний розмір: 5 МБ.");
                }
                if (!coverImage.ContentType.StartsWith("image/"))
                {
                    return BadRequest("Дозволені лише зображення.");
                }

                var fileName = $"album-{Guid.NewGuid()}{Path.GetExtension(coverImage.FileName)}";
                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
                Directory.CreateDirectory(Path.Combine(_environment.WebRootPath, "images"));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(existingAlbum.CoverImageUrl))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingAlbum.CoverImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                existingAlbum.CoverImageUrl = $"/images/{fileName}";
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlbumExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            if (_albumService != null)
            {
                var deleted = await _albumService.DeleteAlbumAsync(id);
                if (!deleted) return NotFound();
                return NoContent();
            }

            var album = await _context.Albums.FindAsync(id);
            if (album == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(album.CoverImageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, album.CoverImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Albums/5/favorite
        [HttpPost("{albumId}/favorite")]
        public async Task<IActionResult> AddToFavorites(int albumId, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("UserId є обов'язковим і має бути більше 0.");
            }

            if (_albumService != null)
            {
                try
                {
                    var favorite = await _albumService.AddToFavoritesAsync(albumId, userId);
                    if (favorite == null) return NotFound("Альбом не знайдено.");
                    return Ok(favorite);
                }
                catch (InvalidOperationException ex)
                {
                    return Conflict(ex.Message);
                }
            }

            var album = await _context.Albums.FindAsync(albumId);
            if (album == null)
            {
                return NotFound("Альбом не знайдено.");
            }

            var existingFavorite = await _context.FavoriteAlbums
                .FirstOrDefaultAsync(f => f.AlbumId == albumId && f.UserId == userId);
            if (existingFavorite != null)
            {
                return Conflict("Альбом уже в улюблених.");
            }

            var fav = new FavoriteAlbum
            {
                AlbumId = albumId,
                UserId = userId,
                Rating = 0
            };

            _context.FavoriteAlbums.Add(fav);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при додаванні до улюблених: {ex.Message}");
                return StatusCode(500, $"Помилка при додаванні до улюблених: {ex.Message}");
            }

            return Ok(fav);
        }

        // DELETE: api/Albums/5/favorite
        [HttpDelete("{albumId}/favorite")]
        public async Task<IActionResult> RemoveFromFavorites(int albumId, [FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("UserId є обов'язковим і має бути більше 0.");
            }

            if (_albumService != null)
            {
                var removed = await _albumService.RemoveFromFavoritesAsync(albumId, userId);
                if (!removed) return NotFound("Альбом не знайдено в улюблених.");
                return NoContent();
            }

            var favorite = await _context.FavoriteAlbums
                .FirstOrDefaultAsync(f => f.AlbumId == albumId && f.UserId == userId);
            if (favorite == null)
            {
                return NotFound("Альбом не знайдено в улюблених.");
            }

            _context.FavoriteAlbums.Remove(favorite);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при видаленні з улюблених: {ex.Message}");
                return StatusCode(500, $"Помилка при видаленні з улюблених: {ex.Message}");
            }

            return NoContent();
        }

        // POST: api/FavoriteAlbums/rate/{id}
        [HttpPost("rate/{id}")]
        public async Task<IActionResult> RateAlbum(int id, [FromBody] int rating)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest("Оцінка має бути від 1 до 5.");
            }

            if (_albumService != null)
            {
                var favorite = await _albumService.RateAlbumAsync(id, rating);
                if (favorite == null) return NotFound("Улюблений альбом не знайдено.");
                return Ok(new { favorite.Id, favorite.AlbumId, favorite.UserId, favorite.Rating });
            }

            var fav = await _context.FavoriteAlbums.FindAsync(id);
            if (fav == null)
            {
                return NotFound("Улюблений альбом не знайдено.");
            }

            fav.Rating = rating;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при оновленні оцінки: {ex.Message}");
                return StatusCode(500, $"Помилка при оновленні оцінки: {ex.Message}");
            }

            return Ok(new { fav.Id, fav.AlbumId, fav.UserId, fav.Rating });
        }

        private bool AlbumExists(int id)
        {
            return _context.Albums.Any(e => e.Id == id);
        }
    }

    public class AlbumDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ReleaseYear { get; set; }
        public int ArtistId { get; set; }
        public string ArtistName { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}