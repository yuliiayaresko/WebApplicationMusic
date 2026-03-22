using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class UsersControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public UsersControllerTests(TestFixture fixture) => _fixture = fixture;

        // ── GetUsers ──
        [Fact]
        public async Task GetUsers_ReturnsAllUsers()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Users.Add(new User { Username = "user1", Email = "u1@test.com" });
            await context.SaveChangesAsync();

            var controller = new UsersController(context);
            var result = await controller.GetUsers();

            Assert.NotEmpty(result.Value);
        }

        // ── GetUser — happy path ──
        [Fact]
        public async Task GetUser_ExistingId_ReturnsUser()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var user = new User { Username = "found", Email = "found@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UsersController(context);
            var result = await controller.GetUser(user.Id);

            Assert.Equal("found", result.Value.Username);
        }

        // ── GetUser — NotFound ──
        [Fact]
        public async Task GetUser_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new UsersController(context);

            var result = await controller.GetUser(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── PostUser ──
        [Fact]
        public async Task PostUser_ValidData_ReturnsCreated()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new UsersController(context);
            var user = new User { Username = "newuser", Email = "new@test.com" };

            var result = await controller.PostUser(user);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(created.Value);
        }

        // ── DeleteUser — happy path ──
        [Fact]
        public async Task DeleteUser_Existing_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var user = new User { Username = "todel", Email = "del@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UsersController(context);
            var result = await controller.DeleteUser(user.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Users.FindAsync(user.Id));
        }

        // ── DeleteUser — NotFound ──
        [Fact]
        public async Task DeleteUser_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new UsersController(context);

            var result = await controller.DeleteUser(99999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ── GetRecentPlaylists — user not found ──
        [Fact]
        public async Task GetRecentPlaylists_UserNotFound_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new UsersController(context);

            var result = await controller.GetRecentPlaylists(99999);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // ── GetRecentPlaylists — user без плейлистів ──
        [Fact]
        public async Task GetRecentPlaylists_NoPlaylists_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var user = new User { Username = "noplaylists", Email = "np@test.com", Playlists = new List<Playlist>() };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UsersController(context);
            var result = await controller.GetRecentPlaylists(user.Id);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // ── GetRecentPlaylists — є нові плейлисти ──
        [Fact]
        public async Task GetRecentPlaylists_HasRecentPlaylist_ReturnsIt()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var user = new User
            {
                Username = "withplaylists",
                Email = "wp@test.com",
                Playlists = new List<Playlist>
                {
                    new Playlist { Name = "Recent", CreatedDate = DateTime.UtcNow.AddDays(-3) },
                    new Playlist { Name = "Old",    CreatedDate = DateTime.UtcNow.AddMonths(-5) }
                }
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UsersController(context);
            var result = await controller.GetRecentPlaylists(user.Id, months: 1);

            var playlists = Assert.IsAssignableFrom<IEnumerable<Playlist>>(result.Value);
            Assert.Single(playlists);
            Assert.Equal("Recent", playlists.First().Name);
        }

        // ── GetRecentPlaylists — всі плейлисти старі ──
        [Fact]
        public async Task GetRecentPlaylists_AllOld_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var user = new User
            {
                Username = "oldplaylists",
                Email = "old@test.com",
                Playlists = new List<Playlist>
                {
                    new Playlist { Name = "VeryOld", CreatedDate = DateTime.UtcNow.AddMonths(-6) }
                }
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UsersController(context);
            var result = await controller.GetRecentPlaylists(user.Id, months: 1);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task RemoveFromFavorites_Valid_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Title = "Remove Fav", ArtistName = "Band", ReleaseYear = 2020 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            context.FavoriteAlbums.Add(new FavoriteAlbum { AlbumId = album.Id, UserId = 55, Rating = 3 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);
            var result = await controller.RemoveFromFavorites(album.Id, 55);

            Assert.IsType<NoContentResult>(result);
            var gone = await context.FavoriteAlbums
                .FirstOrDefaultAsync(f => f.AlbumId == album.Id && f.UserId == 55);
            Assert.Null(gone);
        }

        // ── UsersController PutUser — happy path (+ UserExists) ──
        [Fact]
        public async Task PutUser_ValidData_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var user = new User { Username = "original", Email = "orig@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            context.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            var controller = new UsersController(context);
            var updated = new User { Id = user.Id, Username = "updated", Email = "updated@test.com" };

            var result = await controller.PutUser(user.Id, updated);

            Assert.IsType<NoContentResult>(result);
        }

        // ── UsersController PutUser — id mismatch ──
        [Fact]
        public async Task PutUser_IdMismatch_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new UsersController(context);
            var user = new User { Id = 999, Username = "x", Email = "x@x.com" };

            var result = await controller.PutUser(1, user);

            Assert.IsType<BadRequestResult>(result);
        }
    }
}