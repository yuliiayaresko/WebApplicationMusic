using System.ComponentModel.DataAnnotations;
using WebApplicationMusic.Models;
using WebApplicationMusic.Tests.Fixtures;
using Xunit;

namespace WebApplicationMusic.Tests
{
    [Collection("MusicTestCollection")]
    public class ParameterizedTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;

        public ParameterizedTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        
        [Theory]
        [InlineData("Bohemian Rhapsody", "Queen", "5:55", true)]
        [InlineData("", "Unknown", "3:30", false)]
        public void Song_Validation_With_InlineData(string title, string artist, string duration, bool shouldBeValid)
        {
            var song = new Song { Title = title, Artist = artist, Duration = duration };

            var context = new ValidationContext(song);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(song, context, results, true);

            Assert.Equal(shouldBeValid, isValid);
        }

        
        public static IEnumerable<object[]> SongTestData()
        {
            yield return new object[] { new Song { Title = "Stairway to Heaven", Artist = "Led Zeppelin", Duration = "8:02" }, true };
            yield return new object[] { new Song { Title = "", Artist = "Unknown", Duration = "0:00" }, false };
        }

        [Theory]
        [MemberData(nameof(SongTestData))]
        public void Song_Validation_With_MemberData(Song song, bool shouldBeValid)
        {
            var context = new ValidationContext(song);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(song, context, results, true);

            Assert.Equal(shouldBeValid, isValid);

            if (!shouldBeValid)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    if (string.IsNullOrEmpty(song.Title))
                        throw new ArgumentException("Назва пісні обов’язкова");
                });
            }
        }
    }
}