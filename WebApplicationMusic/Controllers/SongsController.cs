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

namespace WebApplicationMusic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SongsController : ControllerBase
    {
        private readonly MusicAPIContext _context;
        private readonly IWebHostEnvironment _environment;

        public SongsController(MusicAPIContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Songs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Song>>> GetSongs()
        {
            var songs = await _context.Songs
                .Where(s => s.Title != null)
                .Select(s => new Song
                {
                    Id = s.Id,
                    Title = s.Title,
                    AlbumId = s.AlbumId,
                    Album = s.Album,
                    AudioUrl = s.AudioUrl,
                    Artist = s.Artist
                })
                .ToListAsync();
            return songs;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Song>> GetSong(int id)
        {
            var song = await _context.Songs
                .Where(s => s.Id == id)
                .Select(s => new Song
                {
                    Id = s.Id,
                    Title = s.Title,
                    AlbumId = s.AlbumId,
                    Album = s.Album,
                    AudioUrl = s.AudioUrl,
                    Artist = s.Artist
                })
                .FirstOrDefaultAsync();

            if (song == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(song.Title) || song.AlbumId == null)
            {
                return BadRequest("Дані пісні пошкоджені.");
            }

            return song;
        }

        // PUT: api/Songs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSong(int id, [FromForm] SongDto songDto)
        {
            if (id != songDto.Id)
            {
                return BadRequest("Id у маршруті не збігається з Id у тілі запиту.");
            }

            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Оновлюємо поля
            song.Title = songDto.Title;
            song.AlbumId = songDto.AlbumId;
            song.Artist = songDto.Artist;

            // Обробка нового аудіофайлу, якщо завантажено
            if (songDto.AudioFile != null && songDto.AudioFile.Length > 0)
            {
                // Видаляємо старий файл, якщо він існує
                if (!string.IsNullOrEmpty(song.AudioUrl))
                {
                    string oldFilePath = Path.Combine(_environment.WebRootPath, song.AudioUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
                // Зберігаємо новий файл
                song.AudioUrl = await SaveAudioFile(songDto.AudioFile);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SongExists(id))
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

        // POST: api/Songs
        [HttpPost]
        public async Task<ActionResult<Song>> PostSong([FromForm] SongDto songDto)
        {
            if (string.IsNullOrEmpty(songDto.Title))
            {
                return BadRequest("Назва пісні обов’язкова!");
            }

            string? audioPath = null;
            if (songDto.AudioFile != null && songDto.AudioFile.Length > 0)
            {
                audioPath = await SaveAudioFile(songDto.AudioFile);
            }

            var song = new Song
            {
                Title = songDto.Title,
                AlbumId = songDto.AlbumId,
                AudioUrl = audioPath,
                Artist = songDto.Artist
            };

            _context.Songs.Add(song);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSong", new { id = song.Id }, song);
        }

        // DELETE: api/Songs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSong(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Видаляємо аудіофайл, якщо він існує
            if (!string.IsNullOrEmpty(song.AudioUrl))
            {
                string filePath = Path.Combine(_environment.WebRootPath, song.AudioUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SongExists(int id)
        {
            return _context.Songs.Any(e => e.Id == id);
        }

        private async Task<string> SaveAudioFile(IFormFile audioFile)
        {
            string audioFolder = Path.Combine(_environment.WebRootPath, "audio");
            if (!Directory.Exists(audioFolder))
            {
                Directory.CreateDirectory(audioFolder);
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
            string filePath = Path.Combine(audioFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            return $"/audio/{fileName}";
        }
    }

    public class SongDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int? AlbumId { get; set; }
        public string? Artist { get; set; }
        public IFormFile? AudioFile { get; set; }
    }
}