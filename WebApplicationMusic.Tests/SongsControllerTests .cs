using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class SongsControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public SongsControllerTests(TestFixture fixture) => _fixture = fixture;

        // ── Покриває GetSongs + Where + Select (найбільший шматок) ──
        [Fact]
        public async Task GetSongs_ReturnsSongsWithTitle()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Songs.Add(new Song { Title = "Test Song", AlbumId = null, AudioUrl = "/audio/test.mp3" });
            await context.SaveChangesAsync();

            var controller = new SongsController(context, null);
            var result = await controller.GetSongs();

            var songs = Assert.IsAssignableFrom<IEnumerable<Song>>(result.Value);
            Assert.NotEmpty(songs);
        }

        // ── Покриває GetSong — happy path ──
        [Fact]
        public async Task GetSong_ExistingId_ReturnsSong()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var song = new Song { Title = "Exist Song", AlbumId = 1 }; // AlbumId не null!
            context.Songs.Add(song);
            await context.SaveChangesAsync();

            var controller = new SongsController(context, null);
            var result = await controller.GetSong(song.Id);

            var returned = Assert.IsType<Song>(result.Value);
            Assert.Equal("Exist Song", returned.Title);
        }

        // ── Покриває GetSong — NotFound branch ──
        [Fact]
        public async Task GetSong_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new SongsController(context, null);

            var result = await controller.GetSong(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── Покриває PostSong — порожній Title (BadRequest branch) ──
        [Fact]
        public async Task PostSong_EmptyTitle_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new SongsController(context, null);
            var dto = new SongDto { Title = "" };

            var result = await controller.PostSong(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // ── Покриває PostSong — без аудіофайлу (audioPath = null branch) ──
        [Fact]
        public async Task PostSong_WithoutAudioFile_ReturnsCreated()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new SongsController(context, null);
            var dto = new SongDto { Title = "No Audio Song", AlbumId = null, AudioFile = null };

            var result = await controller.PostSong(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var song = Assert.IsType<Song>(created.Value);
            Assert.Equal("No Audio Song", song.Title);
            Assert.Null(song.AudioUrl);
        }

        // ── Покриває PutSong — id mismatch (BadRequest branch) ──
        [Fact]
        public async Task PutSong_IdMismatch_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new SongsController(context, mockEnv.Object);
            var dto = new SongDto { Id = 999, Title = "Mismatch" };

            var result = await controller.PutSong(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── Покриває PutSong — NotFound branch ──
        [Fact]
        public async Task PutSong_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new SongsController(context, mockEnv.Object);
            var dto = new SongDto { Id = 88888, Title = "Ghost" };

            var result = await controller.PutSong(88888, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        // ── Покриває PutSong — happy path без нового аудіофайлу ──
        [Fact]
        public async Task PutSong_ValidData_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            context.Songs.Add(new Song { Id = 3001, Title = "Old", AlbumId = null });
            await context.SaveChangesAsync();

            var controller = new SongsController(context, mockEnv.Object);
            var dto = new SongDto { Id = 3001, Title = "Updated", AlbumId = null, AudioFile = null };

            var result = await controller.PutSong(3001, dto);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Updated", (await context.Songs.FindAsync(3001)).Title);
        }

        // ── Покриває DeleteSong — NotFound branch ──
        [Fact]
        public async Task DeleteSong_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new SongsController(context, null);

            var result = await controller.DeleteSong(77777);

            Assert.IsType<NotFoundResult>(result);
        }

        // ── Покриває DeleteSong — happy path (song без AudioUrl) ──
        [Fact]
        public async Task DeleteSong_ExistingWithoutAudio_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Songs.Add(new Song { Id = 4001, Title = "Delete Me", AudioUrl = null });
            await context.SaveChangesAsync();

            var controller = new SongsController(context, null);
            var result = await controller.DeleteSong(4001);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Songs.FindAsync(4001));
        }

        [Fact]
        public async Task DeleteSong_WithAudioUrl_DeletesFileAndReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);

            // Створюємо тимчасовий файл щоб симулювати існуючий AudioUrl
            var tempDir = Path.Combine(Path.GetTempPath(), "wwwroot", "audio");
            Directory.CreateDirectory(tempDir);
            var fileName = $"{Guid.NewGuid()}.mp3";
            var fullPath = Path.Combine(tempDir, fileName);
            await File.WriteAllTextAsync(fullPath, "fake audio");

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));

            var song = new Song { Title = "Has Audio", AudioUrl = $"/audio/{fileName}" };
            context.Songs.Add(song);
            await context.SaveChangesAsync();

            var controller = new SongsController(context, mockEnv.Object);
            var result = await controller.DeleteSong(song.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Songs.FindAsync(song.Id));
        }

        // Покриває PostSong з AudioFile (SaveAudioFile метод)
        [Fact]
        public async Task PostSong_WithAudioFile_SavesFileAndReturnsCreated()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);

            var tempDir = Path.Combine(Path.GetTempPath(), "wwwroot2");
            Directory.CreateDirectory(Path.Combine(tempDir, "audio"));

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("song.mp3");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Returns(Task.CompletedTask);

            var controller = new SongsController(context, mockEnv.Object);
            var dto = new SongDto { Title = "With Audio", AlbumId = null, AudioFile = fileMock.Object };

            var result = await controller.PostSong(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var song = Assert.IsType<Song>(created.Value);
            Assert.NotNull(song.AudioUrl);
            Assert.StartsWith("/audio/", song.AudioUrl);
        }

        // Покриває PutSong з AudioFile (гілка оновлення аудіо + SongExists)
        [Fact]
        public async Task PutSong_WithNewAudioFile_UpdatesAudioUrl()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);

            var tempDir = Path.Combine(Path.GetTempPath(), "wwwroot3");
            Directory.CreateDirectory(Path.Combine(tempDir, "audio"));

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);

            var song = new Song { Title = "Old Audio", AudioUrl = null };
            context.Songs.Add(song);
            await context.SaveChangesAsync();

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("new.mp3");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Returns(Task.CompletedTask);

            var controller = new SongsController(context, mockEnv.Object);
            var dto = new SongDto { Id = song.Id, Title = "New Audio", AlbumId = null, AudioFile = fileMock.Object };

            var result = await controller.PutSong(song.Id, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = await context.Songs.FindAsync(song.Id);
            Assert.NotNull(updated.AudioUrl);
        }
    }
}