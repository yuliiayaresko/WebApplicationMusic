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
    public class AlbumsControllerTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;

        public AlbumsControllerTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        // ──────────────────────────────────────────
        // GET: api/Albums
        // ──────────────────────────────────────────

        [Fact]
        public async Task GetAlbums_ReturnsAllAlbums()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Albums.Add(new Album { Title = "Album A", ArtistName = "Artist A", ReleaseYear = 2000 });
            context.Albums.Add(new Album { Title = "Album B", ArtistName = "Artist B", ReleaseYear = 2001 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetAlbums();

            // Assert
            var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result.Value);
            Assert.NotEmpty(albums);
        }

        // ──────────────────────────────────────────
        // GET: api/Albums/{id}
        // ──────────────────────────────────────────

        [Fact]
        public async Task GetAlbum_ExistingId_ReturnsAlbum()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 200, Title = "Specific Album", ArtistName = "Solo", ReleaseYear = 2010 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetAlbum(200);

            // Assert
            var returned = Assert.IsType<Album>(result.Value);
            Assert.Equal("Specific Album", returned.Title);
        }

        [Fact]
        public async Task GetAlbum_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetAlbum(99999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ──────────────────────────────────────────
        // GET: api/Albums/search
        // ──────────────────────────────────────────

        [Fact]
        public async Task SearchAlbums_EmptyQuery_ReturnsAllAlbums()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Albums.Add(new Album { Title = "Alpha", ArtistName = "Art1", ReleaseYear = 2020 });
            context.Albums.Add(new Album { Title = "Beta", ArtistName = "Art2", ReleaseYear = 2021 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.SearchAlbums(string.Empty);

            // Assert
            var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result.Value);
            Assert.NotEmpty(albums);
        }

        [Fact]
        public async Task SearchAlbums_ByArtistName_ReturnsMatchingAlbums()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Albums.Add(new Album { Title = "Jazz Night", ArtistName = "Miles Davis", ReleaseYear = 1959 });
            context.Albums.Add(new Album { Title = "Rock Hard", ArtistName = "AC/DC", ReleaseYear = 1980 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.SearchAlbums("Miles");

            // Assert
            var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result.Value);
            Assert.Single(albums);
            Assert.Equal("Miles Davis", albums.First().ArtistName);
        }

        [Fact]
        public async Task SearchAlbums_NoMatch_ReturnsEmptyList()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Albums.Add(new Album { Title = "Only One", ArtistName = "Some Artist", ReleaseYear = 2022 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.SearchAlbums("xyznotexist");

            // Assert
            var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result.Value);
            Assert.Empty(albums);
        }

        // ──────────────────────────────────────────
        // GET: api/Albums/favorites
        // ──────────────────────────────────────────

        [Fact]
        public async Task GetFavorites_InvalidUserId_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetFavorites(0);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetFavorites_ValidUserId_ReturnsFavoritesList()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 300, Title = "Fav", ArtistName = "Band", ReleaseYear = 2015 };
            context.Albums.Add(album);
            context.FavoriteAlbums.Add(new FavoriteAlbum { AlbumId = 300, UserId = 10, Rating = 4 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetFavorites(10);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        // ──────────────────────────────────────────
        // GET: api/Albums/user/{userId}
        // ──────────────────────────────────────────

        [Fact]
        public async Task GetFavoriteAlbumsByUser_InvalidUserId_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetFavoriteAlbumsByUser(-5);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetFavoriteAlbumsByUser_ValidUser_ReturnsData()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 400, Title = "User Fav", ArtistName = "Band", ReleaseYear = 2018 };
            context.Albums.Add(album);
            context.FavoriteAlbums.Add(new FavoriteAlbum { AlbumId = 400, UserId = 20, Rating = 5 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.GetFavoriteAlbumsByUser(20);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        // ──────────────────────────────────────────
        // POST: api/Albums
        // ──────────────────────────────────────────

        [Fact]
        public async Task PostAlbum_WithoutImage_ReturnsCreated()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new AlbumsController(context, mockEnv.Object);

            var dto = new AlbumDto { Title = "New Album", ArtistName = "New Artist", ReleaseYear = 2023 };

            // Act
            var result = await controller.PostAlbum(dto, null);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var album = Assert.IsType<Album>(created.Value);
            Assert.Equal("New Album", album.Title);
        }

        [Fact]
        public async Task PostAlbum_WithInvalidImageSize_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new AlbumsController(context, mockEnv.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.Length).Returns(6 * 1024 * 1024); // 6 МБ — понад ліміт

            var dto = new AlbumDto { Title = "Too Big" };

            // Act
            var result = await controller.PostAlbum(dto, fileMock.Object);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostAlbum_WithInvalidImageType_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new AlbumsController(context, mockEnv.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.Length).Returns(1 * 1024 * 1024); // 1 МБ — розмір OK
            fileMock.Setup(_ => _.ContentType).Returns("application/pdf"); // Невірний тип

            var dto = new AlbumDto { Title = "Wrong Type" };

            // Act
            var result = await controller.PostAlbum(dto, fileMock.Object);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // ──────────────────────────────────────────
        // PUT: api/Albums/{id}
        // ──────────────────────────────────────────

        [Fact]
        public async Task PutAlbum_MismatchedId_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new AlbumsController(context, mockEnv.Object);

            var dto = new AlbumDto { Id = 999, Title = "Mismatch" };

            // Act
            var result = await controller.PutAlbum(1, dto, null); // id=1 ≠ dto.Id=999

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutAlbum_NonExistingAlbum_ReturnsNotFound()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new AlbumsController(context, mockEnv.Object);

            var dto = new AlbumDto { Id = 88888, Title = "Ghost Album" };

            // Act
            var result = await controller.PutAlbum(88888, dto, null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PutAlbum_ValidData_UpdatesAlbumAndReturnsNoContent()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var album = new Album { Id = 500, Title = "Old Title", ArtistName = "Old", ReleaseYear = 2000 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, mockEnv.Object);
            var dto = new AlbumDto { Id = 500, Title = "New Title", ArtistName = "New", ReleaseYear = 2024 };

            // Act
            var result = await controller.PutAlbum(500, dto, null);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updated = await context.Albums.FindAsync(500);
            Assert.Equal("New Title", updated.Title);
        }

        [Fact]
        public async Task PutAlbum_WithInvalidImageSize_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockEnv = new Mock<IWebHostEnvironment>();
            var album = new Album { Id = 501, Title = "Has Image", ArtistName = "Band", ReleaseYear = 2020 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, mockEnv.Object);
            var dto = new AlbumDto { Id = 501, Title = "Has Image", ArtistName = "Band", ReleaseYear = 2020 };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.Length).Returns(10 * 1024 * 1024); // 10 МБ

            // Act
            var result = await controller.PutAlbum(501, dto, fileMock.Object);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ──────────────────────────────────────────
        // DELETE: api/Albums/{id}
        // ──────────────────────────────────────────

        [Fact]
        public async Task DeleteAlbum_ExistingAlbum_RemovesFromDatabase()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 600, Title = "To Delete", ArtistName = "Band", ReleaseYear = 2019 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.DeleteAlbum(600);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Albums.FindAsync(600));
        }

        [Fact]
        public async Task DeleteAlbum_NonExistingAlbum_ReturnsNotFound()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.DeleteAlbum(77777);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // ──────────────────────────────────────────
        // POST: api/Albums/{albumId}/favorite
        // ──────────────────────────────────────────

        [Fact]
        public async Task AddToFavorites_InvalidUserId_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.AddToFavorites(1, 0);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddToFavorites_AlbumNotFound_ReturnsNotFound()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.AddToFavorites(99999, 1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddToFavorites_AlreadyFavorited_ReturnsConflict()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 700, Title = "Dup Fav", ArtistName = "Band", ReleaseYear = 2021 };
            context.Albums.Add(album);
            context.FavoriteAlbums.Add(new FavoriteAlbum { AlbumId = 700, UserId = 5, Rating = 0 });
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.AddToFavorites(700, 5); // вже існує

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task AddToFavorites_ValidRequest_ReturnsOkWithFavorite()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 800, Title = "New Fav", ArtistName = "Artist", ReleaseYear = 2022 };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.AddToFavorites(800, 9);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var fav = Assert.IsType<FavoriteAlbum>(ok.Value);
            Assert.Equal(800, fav.AlbumId);
            Assert.Equal(9, fav.UserId);
        }

        // ──────────────────────────────────────────
        // DELETE: api/Albums/{albumId}/favorite
        // ──────────────────────────────────────────

        [Fact]
        public async Task RemoveFromFavorites_InvalidUserId_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.RemoveFromFavorites(1, 0);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RemoveFromFavorites_NotInFavorites_ReturnsNotFound()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.RemoveFromFavorites(99999, 1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ──────────────────────────────────────────
        // POST: api/Albums/rate/{id}
        // ──────────────────────────────────────────

        [Fact]
        public async Task RateAlbum_RatingTooLow_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.RateAlbum(1, 0); // 0 < мінімум 1

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RateAlbum_RatingTooHigh_ReturnsBadRequest()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.RateAlbum(1, 6); // 6 > максимум 5

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RateAlbum_FavoriteNotFound_ReturnsNotFound()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.RateAlbum(99999, 4);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RateAlbum_ValidRating_UpdatesAndReturnsOk()
        {
            // Arrange
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var album = new Album { Id = 1000, Title = "Rate Me", ArtistName = "Band", ReleaseYear = 2023 };
            context.Albums.Add(album);
            var fav = new FavoriteAlbum { AlbumId = 1000, UserId = 2, Rating = 0 };
            context.FavoriteAlbums.Add(fav);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, null);

            // Act
            var result = await controller.RateAlbum(fav.Id, 5);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            var updated = await context.FavoriteAlbums.FindAsync(fav.Id);
            Assert.Equal(5, updated.Rating);
        }

        [Fact]
        public async Task PutAlbum_WithNewCoverImage_UpdatesImageUrl()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);

            var tempDir = Path.Combine(Path.GetTempPath(), "wwwrootAlbum");
            Directory.CreateDirectory(Path.Combine(tempDir, "images"));

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);

            var album = new Album { Title = "Old", ArtistName = "Band", ReleaseYear = 2020, CoverImageUrl = null };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            context.Entry(album).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("cover.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);

            var controller = new AlbumsController(context, mockEnv.Object);
            var dto = new AlbumDto { Id = album.Id, Title = "Updated", ArtistName = "Band", ReleaseYear = 2020 };

            var result = await controller.PutAlbum(album.Id, dto, fileMock.Object);

            Assert.IsType<NoContentResult>(result);
            var updated = await context.Albums.FindAsync(album.Id);
            Assert.NotNull(updated.CoverImageUrl);
        }

        [Fact]
        public async Task PutAlbum_WithExistingCoverImage_DeletesOldFile()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);

            var tempDir = Path.Combine(Path.GetTempPath(), "wwwrootAlbum2");
            Directory.CreateDirectory(Path.Combine(tempDir, "images"));

            // Створюємо старий файл
            var oldFileName = "old-cover.jpg";
            var oldFilePath = Path.Combine(tempDir, "images", oldFileName);
            await File.WriteAllTextAsync(oldFilePath, "old image");

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);

            var album = new Album
            {
                Title = "Has Cover",
                ArtistName = "Band",
                ReleaseYear = 2020,
                CoverImageUrl = $"/images/{oldFileName}"
            };
            context.Albums.Add(album);
            await context.SaveChangesAsync();
            context.Entry(album).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("new-cover.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);

            var controller = new AlbumsController(context, mockEnv.Object);
            var dto = new AlbumDto { Id = album.Id, Title = "Has Cover", ArtistName = "Band", ReleaseYear = 2020 };

            var result = await controller.PutAlbum(album.Id, dto, fileMock.Object);

            Assert.IsType<NoContentResult>(result);
            Assert.False(File.Exists(oldFilePath)); // старий файл видалено
        }

        // ── DeleteAlbum — з існуючим CoverImageUrl (покриває гілку видалення файлу) ──
        [Fact]
        public async Task DeleteAlbum_WithCoverImage_DeletesFileAndAlbum()
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);

            var tempDir = Path.Combine(Path.GetTempPath(), "wwwrootDelete");
            Directory.CreateDirectory(Path.Combine(tempDir, "images"));

            var fileName = "delete-cover.jpg";
            var filePath = Path.Combine(tempDir, "images", fileName);
            await File.WriteAllTextAsync(filePath, "image data");

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);

            var album = new Album
            {
                Title = "Delete With Image",
                ArtistName = "Band",
                ReleaseYear = 2021,
                CoverImageUrl = $"/images/{fileName}"
            };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            var controller = new AlbumsController(context, mockEnv.Object);
            var result = await controller.DeleteAlbum(album.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Albums.FindAsync(album.Id));
            Assert.False(File.Exists(filePath));
        }
    }
}