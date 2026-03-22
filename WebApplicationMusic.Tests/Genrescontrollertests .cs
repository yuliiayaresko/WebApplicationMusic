using Microsoft.AspNetCore.Mvc;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class GenresControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public GenresControllerTests(TestFixture fixture) => _fixture = fixture;

        // ── GetGenres ──
        [Fact]
        public async Task GetGenres_ReturnsAll()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Genres.Add(new Genre { Name = "Rock" });
            await context.SaveChangesAsync();

            var controller = new GenresController(context);
            var result = await controller.GetGenres();

            Assert.NotEmpty(result.Value);
        }

        // ── GetGenre — happy path ──
        [Fact]
        public async Task GetGenre_ExistingId_ReturnsGenre()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var genre = new Genre { Name = "Jazz" };
            context.Genres.Add(genre);
            await context.SaveChangesAsync();

            var controller = new GenresController(context);
            var result = await controller.GetGenre(genre.Id);

            Assert.Equal("Jazz", result.Value.Name);
        }

        // ── GetGenre — NotFound ──
        [Fact]
        public async Task GetGenre_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new GenresController(context);

            var result = await controller.GetGenre(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── PostGenre ──
        [Fact]
        public async Task PostGenre_ValidData_ReturnsCreated()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new GenresController(context);
            var genre = new Genre { Name = "Blues" };

            var result = await controller.PostGenre(genre);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(created.Value);
        }

        // ── DeleteGenre — happy path ──
        [Fact]
        public async Task DeleteGenre_Existing_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var genre = new Genre { Name = "ToDelete" };
            context.Genres.Add(genre);
            await context.SaveChangesAsync();

            var controller = new GenresController(context);
            var result = await controller.DeleteGenre(genre.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Genres.FindAsync(genre.Id));
        }

        // ── DeleteGenre — NotFound ──
        [Fact]
        public async Task DeleteGenre_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new GenresController(context);

            var result = await controller.DeleteGenre(99999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ── PutGenre — id mismatch ──чч
        [Fact]
        public async Task PutGenre_IdMismatch_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new GenresController(context);
            var genre = new Genre { Id = 999, Name = "Mismatch" };

            var result = await controller.PutGenre(1, genre);

            Assert.IsType<BadRequestResult>(result);
        }
    }
}