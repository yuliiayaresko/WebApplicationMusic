using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationMusic;
using WebApplicationMusic.Models;

namespace WebApplicationMusic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteAlbumsController : ControllerBase
    {
        private readonly MusicAPIContext _context;

        public FavoriteAlbumsController(MusicAPIContext context)
        {
            _context = context;
        }

        // GET: api/FavoriteAlbums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FavoriteAlbum>>> GetFavoriteAlbums()
        {
            return await _context.FavoriteAlbums
                .Include(f => f.Album)
                .ToListAsync();
        }

        // GET: api/FavoriteAlbums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FavoriteAlbum>> GetFavoriteAlbum(int id)
        {
            var favoriteAlbum = await _context.FavoriteAlbums
                .Include(f => f.Album)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (favoriteAlbum == null)
            {
                return NotFound();
            }

            return favoriteAlbum;
        }

        // GET: api/FavoriteAlbums/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserFavoriteAlbums(int userId)
        {
            var favoriteAlbums = await _context.FavoriteAlbums
                .Where(f => f.UserId == userId)
                .Include(f => f.Album)
                    .ThenInclude(a => a.Artist)
                .Select(f => new
                {
                    Id = f.Id, // Ідентифікатор запису FavoriteAlbum
                    AlbumId = f.Album.Id,
                    f.Album.Title,
                    f.Album.ReleaseYear,
                    f.Album.CoverImageUrl,
                    ArtistName = f.Album.Artist != null ? f.Album.Artist.Name : f.Album.ArtistName,
                    Songs = f.Album.Songs.Select(s => new { s.Title, s.Duration })
                })
                .ToListAsync();

            Console.WriteLine($"Повернуто {favoriteAlbums.Count} улюблених альбомів для userId: {userId}");

            return favoriteAlbums;
        }

        // POST: api/FavoriteAlbums/rate/5
        [HttpPost("rate/{id}")]
        public async Task<IActionResult> RateFavoriteAlbum(int id, [FromBody] int rating)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest("Оцінка має бути від 1 до 5.");
            }

            var favoriteAlbum = await _context.FavoriteAlbums.FindAsync(id);
            if (favoriteAlbum == null)
            {
                return NotFound("Улюблений альбом не знайдено.");
            }

            favoriteAlbum.Rating = rating;
            _context.Entry(favoriteAlbum).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FavoriteAlbumExists(id))
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

        private bool FavoriteAlbumExists(int id)
        {
            return _context.FavoriteAlbums.Any(e => e.Id == id);
        }
    }
}