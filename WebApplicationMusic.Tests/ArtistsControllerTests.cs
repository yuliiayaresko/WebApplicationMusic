using Microsoft.AspNetCore.Mvc;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class ArtistsControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public ArtistsControllerTests(TestFixture fixture) => _fixture = fixture;

        // ── Покриває GetArtists ──
        [Fact]
        public async Task GetArtists_ReturnsAll()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Artists.Add(new Artist { Id = 5001, Name = "Artist X"});
            await context.SaveChangesAsync();

            var controller = new ArtistsController(context);
            var result = await controller.GetArtists();

            Assert.NotEmpty(result.Value);
        }

        // ── Покриває GetArtist — happy path ──
        [Fact]
        public async Task GetArtist_ExistingId_ReturnsArtist()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Artists.Add(new Artist { Id = 5002, Name = "Artist Y"});
            await context.SaveChangesAsync();

            var controller = new ArtistsController(context);
            var result = await controller.GetArtist(5002);

            Assert.Equal("Artist Y", result.Value.Name);
        }

        // ── Покриває GetArtist — NotFound ──
        [Fact]
        public async Task GetArtist_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new ArtistsController(context);

            var result = await controller.GetArtist(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── Покриває PostArtist ──
        [Fact]
        public async Task PostArtist_ValidData_ReturnsCreated()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new ArtistsController(context);
            var artist = new Artist { Name = "New Artist" };

            var result = await controller.PostArtist(artist);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(created.Value);
        }

        // ── Покриває PutArtist — id mismatch ──
        [Fact]
        public async Task PutArtist_IdMismatch_ReturnsBadRequest()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new ArtistsController(context);
            var artist = new Artist { Id = 999, Name = "Mismatch" };

            var result = await controller.PutArtist(1, artist);

            Assert.IsType<BadRequestResult>(result);
        }

        // ── Покриває DeleteArtist — happy path ──
        [Fact]
        public async Task DeleteArtist_Existing_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Artists.Add(new Artist { Id = 6001, Name = "To Delete" });
            await context.SaveChangesAsync();

            var controller = new ArtistsController(context);
            var result = await controller.DeleteArtist(6001);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Artists.FindAsync(6001));
        }

        // ── Покриває DeleteArtist — NotFound ──
        [Fact]
        public async Task DeleteArtist_NonExisting_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new ArtistsController(context);

            var result = await controller.DeleteArtist(77777);

            Assert.IsType<NotFoundResult>(result);
        }

        // ── Покриває GetRecommendedArtists — артист не знайдений ──
        [Fact]
        public async Task GetRecommendedArtists_ArtistNotFound_ReturnsNotFound()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new ArtistsController(context);

            var result = await controller.GetRecommendedArtists(99999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        public async Task PutArtist_ValidData_ReturnsNoContent()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var artist = new Artist { Name = "Original", Albums = new List<Album>() };
            context.Artists.Add(artist);
            await context.SaveChangesAsync();

            // Detach щоб уникнути конфлікту трекінгу
            context.Entry(artist).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            var controller = new ArtistsController(context);
            var updated = new Artist { Id = artist.Id, Name = "Updated" };

            var result = await controller.PutArtist(artist.Id, updated);

            Assert.IsType<NoContentResult>(result);
        }

        // Покриває GetRecommendedArtists — збіг по кількості альбомів (друга умова Where)
        [Fact]
        public async Task GetRecommendedArtists_SimilarAlbumCount_ReturnsRecommendations()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Artists.RemoveRange(context.Artists.ToList());
            await context.SaveChangesAsync();

            // Base має 3 альбоми, Similar має 2 — різниця 1 (<= 2), Different має 10
            var baseArtist = new Artist
            {
                Name = "Base",
                Albums = new List<Album>
                {
                    new Album { Title = "A1", ReleaseYear = 2020 },
                    new Album { Title = "A2", ReleaseYear = 2021 },
                    new Album { Title = "A3", ReleaseYear = 2022 }
                }
            };
            var similar = new Artist
            {
                Name = "Similar Count",
                Albums = new List<Album>
                {
                    new Album { Title = "B1", ReleaseYear = 2020 },
                    new Album { Title = "B2", ReleaseYear = 2021 }
                }
            };
            var different = new Artist
            {
                Name = "Too Many Albums",
                Albums = new List<Album>
                {
                    new Album { Title = "C1", ReleaseYear = 2020 },
                    new Album { Title = "C2", ReleaseYear = 2020 },
                    new Album { Title = "C3", ReleaseYear = 2020 },
                    new Album { Title = "C4", ReleaseYear = 2020 },
                    new Album { Title = "C5", ReleaseYear = 2020 },
                    new Album { Title = "C6", ReleaseYear = 2020 },
                    new Album { Title = "C7", ReleaseYear = 2020 },
                    new Album { Title = "C8", ReleaseYear = 2020 },
                    new Album { Title = "C9", ReleaseYear = 2020 },
                    new Album { Title = "C10", ReleaseYear = 2020 }
                }
            };

            context.Artists.AddRange(baseArtist, similar, different);
            await context.SaveChangesAsync();

            var controller = new ArtistsController(context);
            var result = await controller.GetRecommendedArtists(baseArtist.Id);

            var artists = Assert.IsAssignableFrom<IEnumerable<Artist>>(result.Value);
            Assert.Contains(artists, a => a.Name == "Similar Count");
        }
    }
}