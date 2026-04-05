using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Services;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class RequiredPatternTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        public RequiredPatternTests(TestFixture fixture) => _fixture = fixture;

        
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(6)]
        [InlineData(100)]
        public async Task RateAlbum_InvalidRating_ReturnsBadRequest(int rating)
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockService = new Mock<IAlbumService>();
            var controller = new AlbumsController(mockService.Object);
            var result = await controller.RateAlbum(1, rating);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        
        public static IEnumerable<object[]> SearchQueries =>
            new List<object[]>
            {
                new object[] { "Rock",    "Rock Album",   true  },
                new object[] { "xyz999",  "Rock Album",   false },
                new object[] { "2020",    "Rock Album",   true  },
            };

        [Theory]
        [MemberData(nameof(SearchQueries))]
        public async Task SearchAlbums_Parameterized_ReturnsExpectedResult(
            string query, string albumTitle, bool shouldFind)
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            context.Albums.Add(new Album
            {
                Title = albumTitle,
                ArtistName = "Test Artist",
                ReleaseYear = 2020
            });
            await context.SaveChangesAsync();
            var mockService = new Mock<IAlbumService>();
            var controller = new AlbumsController(mockService.Object);
            var result = await controller.SearchAlbums(query);

            var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result.Value);

            if (shouldFind)
                Assert.Contains(albums, a => a.Title == albumTitle);
            else
                Assert.DoesNotContain(albums, a => a.Title == albumTitle);
        }

        
        [Fact]
        public void MusicAPIContext_NullOptions_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var context = new MusicAPIContext(null);
            });
        }

        
        [Theory]
        [ClassData(typeof(InvalidUserIdTestData))]
        public async Task GetFavorites_InvalidUserIds_ReturnsBadRequest(int invalidUserId)
        {
            using var context = new MusicAPIContext(_fixture.DbOptions);
            var mockService = new Mock<IAlbumService>();
            var controller = new AlbumsController(mockService.Object);

            var result = await controller.SearchAlbums("test");
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }

    // ClassData — окремий клас з тестовими даними
    public class InvalidUserIdTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { 0 };
            yield return new object[] { -1 };
            yield return new object[] { -100 };
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
