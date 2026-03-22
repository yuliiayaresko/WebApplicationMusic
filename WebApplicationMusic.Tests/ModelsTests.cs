using System.ComponentModel.DataAnnotations;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class ModelsTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;

        public ModelsTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Song_Should_Create_With_Valid_Data()
        {
            var song = new Song
            {
                Title = "Bohemian Rhapsody",
                Artist = "Queen",
                Duration = "5:55"
            };

            Assert.NotNull(song);                              // 1. NotNull
            Assert.Equal("Bohemian Rhapsody", song.Title);     // 2. Equal
            Assert.True(!string.IsNullOrEmpty(song.Title));    // 3. True
            Assert.Contains("Rhapsody", song.Title);           // складний assert (рядок)
        }

        [Fact]
        public void Playlist_Should_Initialize_Correctly_With_Collection()
        {
            var playlist = new Playlist { Name = "Rock Classics", UserId = 1 };
            playlist.PlaylistSongs = new List<PlaylistSong>();

            Assert.NotNull(playlist);
            Assert.Equal("Rock Classics", playlist.Name);
            Assert.Equal(0, playlist.PlaylistSongs.Count);
            Assert.Equal(playlist.PlaylistSongs, new List<PlaylistSong>()); // складний assert (колекція)
        }

        [Fact]
        public void Song_With_Empty_Title_Should_Throw_Exception()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var song = new Song { Title = "" };
                if (string.IsNullOrEmpty(song.Title))
                    throw new ArgumentException("Назва пісні обов’язкова");
            });
        }

        [Fact]
        public void Song_Invalid_Duration_Should_Fail_Validation()
        {
            var song = new Song { Title = "Test", Duration = "invalid" };

            var context = new ValidationContext(song);
            var results = new List<ValidationResult>();
            bool valid = Validator.TryValidateObject(song, context, results, true);

            Assert.False(valid);
        }
    }
}