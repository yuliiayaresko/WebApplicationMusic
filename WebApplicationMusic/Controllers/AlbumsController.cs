using Microsoft.AspNetCore.Mvc;
using WebApplicationMusic.Models;
using WebApplicationMusic.Services;

namespace WebApplicationMusic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly IAlbumService _albumService;
        private readonly ILogger<AlbumsController> _logger;

        public AlbumsController(IAlbumService albumService, ILogger<AlbumsController> logger)
        {
            _albumService = albumService;
            _logger = logger;
        }

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Album>>> GetAlbums()
        {
            var result = await _albumService.GetAllAlbumsAsync();
            return Ok(result);
        }

        // GET: api/Albums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Album>> GetAlbum(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
                return NotFound();
            return Ok(album);
        }

        // GET: api/Albums/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Album>>> SearchAlbums(string query)
        {
            var albums = await _albumService.SearchAlbumsAsync(query);
            return Ok(albums);
        }

        // GET: api/Albums/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavorites([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("UserId є обов'язковим і має бути більше 0.");

            var favorites = await _albumService.GetFavoritesAsync(userId);
            return Ok(favorites);
        }

        // GET: api/Albums/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavoriteAlbumsByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest("UserId є обов'язковим і має бути більше 0.");

            var favorites = await _albumService.GetFavoritesByUserAsync(userId);
            return Ok(favorites);
        }

        // POST: api/Albums
        [HttpPost]
        public async Task<ActionResult<Album>> PostAlbum([FromForm] AlbumDto albumDto, IFormFile? coverImage)
        {
            try
            {
                var album = await _albumService.CreateAlbumAsync(albumDto, coverImage);
                return CreatedAtAction(nameof(GetAlbum), new { id = album.Id }, album);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Albums/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlbum(int id, [FromForm] AlbumDto albumDto, IFormFile? coverImage)
        {
            if (id != albumDto.Id)
                return BadRequest("Id у маршруті не збігається з Id у тілі запиту.");

            try
            {
                var updated = await _albumService.UpdateAlbumAsync(id, albumDto, coverImage);
                return updated ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var deleted = await _albumService.DeleteAlbumAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // POST: api/Albums/{albumId}/favorite
        [HttpPost("{albumId}/favorite")]
        public async Task<IActionResult> AddToFavorites(int albumId, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("UserId є обов'язковим і має бути більше 0.");

            try
            {
                var favorite = await _albumService.AddToFavoritesAsync(albumId, userId);
                if (favorite == null)
                    return NotFound("Альбом не знайдено.");
                return Ok(favorite);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при додаванні до улюблених albumId={AlbumId} userId={UserId}", albumId, userId);
                return StatusCode(500, "Помилка при додаванні до улюблених.");
            }
        }

        // DELETE: api/Albums/{albumId}/favorite
        [HttpDelete("{albumId}/favorite")]
        public async Task<IActionResult> RemoveFromFavorites(int albumId, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("UserId є обов'язковим і має бути більше 0.");

            try
            {
                var removed = await _albumService.RemoveFromFavoritesAsync(albumId, userId);
                return removed ? NoContent() : NotFound("Альбом не знайдено в улюблених.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні з улюблених albumId={AlbumId} userId={UserId}", albumId, userId);
                return StatusCode(500, "Помилка при видаленні з улюблених.");
            }
        }

        // POST: api/Albums/rate/{id}
        [HttpPost("rate/{id}")]
        public async Task<IActionResult> RateAlbum(int id, [FromBody] int rating)
        {
            if (rating < 1 || rating > 5)
                return BadRequest("Оцінка має бути від 1 до 5.");

            try
            {
                var favorite = await _albumService.RateAlbumAsync(id, rating);
                if (favorite == null)
                    return NotFound("Улюблений альбом не знайдено.");
                return Ok(new { favorite.Id, favorite.AlbumId, favorite.UserId, favorite.Rating });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні оцінки favoriteId={Id}", id);
                return StatusCode(500, "Помилка при оновленні оцінки.");
            }
        }
    }
}
