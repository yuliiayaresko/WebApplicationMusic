using Microsoft.AspNetCore.Mvc;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class FavoriteAlbumsControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public FavoriteAlbumsControllerTests(TestFixture fixture) => _fixture = fixture;

        // ── GetFavoriteAlbums ──
        [Fact]
        public async Task GetFavoriteAlbums_ReturnsAll()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Title = "Test", ArtistName = "Band", ReleaseYear = 2020 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            context.FavoriteAlbums.Add(new FavoriteAlbum { AlbumId = album.Id, UserId = 1, Rating = 3 });
            await context.SaveChangesAsync();

            var controller = new FavoriteAlbumsController(context);
            var result = await controller.GetFavoriteAlbums();

            Assert.NotEmpty(result.Value);
        }

        // ── GetFavoriteAlbum — happy path ──
        [Fact]
        public async Task GetFavoriteAlbum_ExistingId_ReturnsFavorite()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Title = "Found", ArtistName = "Art", ReleaseYear = 2021 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            var fav = new FavoriteAlbum { AlbumId = album.Id, UserId = 2, Rating = 5 };
            context.FavoriteAlbums.Add(fav);
            await context.SaveChangesAsync();

            var controller = new FavoriteAlbumsController(context);
            var result = await controller.GetFavoriteAlbum(fav.Id);

            Assert.NotNull(result.Value);
            Assert.Equal(fav.Id, result.Value.Id);
        }

        // ── GetFavoriteAlbum — NotFound ──
        [Fact]
        public async Task GetFavoriteAlbum_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new FavoriteAlbumsController(context);

            var result = await controller.GetFavoriteAlbum(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── GetUserFavoriteAlbums — повертає список ──
        [Fact]
        public async Task GetUserFavoriteAlbums_ValidUser_ReturnsList()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Title = "User Fav", ArtistName = "Band", ReleaseYear = 2022 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            context.FavoriteAlbums.Add(new FavoriteAlbum { AlbumId = album.Id, UserId = 42, Rating = 4 });
            await context.SaveChangesAsync();

            var controller = new FavoriteAlbumsController(context);
            var result = await controller.GetUserFavoriteAlbums(42);

            // Метод повертає список напряму через Value, не через Result
            Assert.NotNull(result.Value);
        }

        // ── RateFavoriteAlbum — rating < 1 ──
        [Fact]
        public async Task RateFavoriteAlbum_TooLow_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new FavoriteAlbumsController(context);

            var result = await controller.RateFavoriteAlbum(1, 0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── RateFavoriteAlbum — rating > 5 ──
        [Fact]
        public async Task RateFavoriteAlbum_TooHigh_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new FavoriteAlbumsController(context);

            var result = await controller.RateFavoriteAlbum(1, 6);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── RateFavoriteAlbum — not found ──
        [Fact]
        public async Task RateFavoriteAlbum_NotFound_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new FavoriteAlbumsController(context);

            var result = await controller.RateFavoriteAlbum(99999, 3);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ── RateFavoriteAlbum — happy path ──
        [Fact]
        public async Task RateFavoriteAlbum_Valid_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Title = "Rate Album", ArtistName = "Band", ReleaseYear = 2020 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            var fav = new FavoriteAlbum { AlbumId = album.Id, UserId = 7, Rating = 0 };
            context.FavoriteAlbums.Add(fav);
            await context.SaveChangesAsync();

            var controller = new FavoriteAlbumsController(context);
            var result = await controller.RateFavoriteAlbum(fav.Id, 4);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(4, (await context.FavoriteAlbums.FindAsync(fav.Id)).Rating);
        }
    }
}