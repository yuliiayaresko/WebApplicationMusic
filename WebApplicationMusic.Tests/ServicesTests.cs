using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class ServicesTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;

        public ServicesTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Song_Duration_Should_Be_String_Format()
        {
            var song = new Song { Title = "Test Song", Duration = "3:45" };

            Assert.NotNull(song.Duration);
            Assert.True(song.Duration.Contains(":"));
            Assert.Equal("3:45", song.Duration);
        }

        [Fact]
        public void Playlist_With_Songs_Collection_Should_Work()
        {
            var playlist = new Playlist { Name = "My Playlist" };
            playlist.PlaylistSongs = new List<PlaylistSong> { new PlaylistSong() };

            Assert.NotEmpty(playlist.PlaylistSongs);
            Assert.Equal(1, playlist.PlaylistSongs.Count);
        }
    }
}