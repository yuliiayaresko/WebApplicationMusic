using Microsoft.AspNetCore.Mvc;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class PlaylistsControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public PlaylistsControllerTests(TestFixture fixture) => _fixture = fixture;

        // ── GetPlaylists ──
        [Fact]
        public async Task GetPlaylists_ReturnsAll()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Playlists.Add(new Playlist { Name = "My Playlist", UserId = 1 });
            await context.SaveChangesAsync();

            var controller = new PlaylistsController(context);
            var result = await controller.GetPlaylists();

            Assert.NotEmpty(result.Value);
        }

        // ── GetPlaylist — happy path ──
        [Fact]
        public async Task GetPlaylist_ExistingId_ReturnsPlaylist()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var playlist = new Playlist { Name = "Found Playlist", UserId = 1 };
            context.Playlists.Add(playlist);
            await context.SaveChangesAsync();

            var controller = new PlaylistsController(context);
            var result = await controller.GetPlaylist(playlist.Id);

            Assert.Equal("Found Playlist", result.Value.Name);
        }

        // ── GetPlaylist — NotFound ──
        [Fact]
        public async Task GetPlaylist_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new PlaylistsController(context);

            var result = await controller.GetPlaylist(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── PostPlaylist ──
        [Fact]
        public async Task PostPlaylist_ValidData_ReturnsCreated()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new PlaylistsController(context);
            var playlist = new Playlist { Name = "New Playlist", UserId = 1 };

            var result = await controller.PostPlaylist(playlist);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<Playlist>(created.Value);
            Assert.Equal("New Playlist", returned.Name);
        }

        // ── DeletePlaylist — happy path ──
        [Fact]
        public async Task DeletePlaylist_Existing_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var playlist = new Playlist { Name = "To Delete", UserId = 1 };
            context.Playlists.Add(playlist);
            await context.SaveChangesAsync();

            var controller = new PlaylistsController(context);
            var result = await controller.DeletePlaylist(playlist.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Playlists.FindAsync(playlist.Id));
        }

        // ── DeletePlaylist — NotFound ──
        [Fact]
        public async Task DeletePlaylist_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new PlaylistsController(context);

            var result = await controller.DeletePlaylist(99999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ── PutPlaylist — id mismatch ──
        [Fact]
        public async Task PutPlaylist_IdMismatch_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new PlaylistsController(context);
            var playlist = new Playlist { Id = 999, Name = "Mismatch", UserId = 1 };

            var result = await controller.PutPlaylist(1, playlist);

            Assert.IsType<BadRequestResult>(result);
        }

        // ── PutPlaylist — happy path ──
        [Fact]
        public async Task PutPlaylist_ValidData_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var playlist = new Playlist { Name = "Old Name", UserId = 1 };
            context.Playlists.Add(playlist);
            await context.SaveChangesAsync();

            // Detach щоб уникнути конфлікту трекінгу
            context.Entry(playlist).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            var controller = new PlaylistsController(context);
            var updated = new Playlist { Id = playlist.Id, Name = "New Name", UserId = 1 };

            var result = await controller.PutPlaylist(playlist.Id, updated);

            Assert.IsType<NoContentResult>(result);
            var fromDb = await context.Playlists.FindAsync(playlist.Id);
            Assert.Equal("New Name", fromDb.Name);
        }
    }
}