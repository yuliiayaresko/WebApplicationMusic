using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;
using WebApplicationMusic.Services;
using Xunit;

namespace WebApplicationMusic.Tests
{
    public class AlbumsControllerMockTests
    {
        private readonly Mock<IAlbumService> _mockService;
        private readonly AlbumsController _controller;

        public AlbumsControllerMockTests()
        {
            _mockService = new Mock<IAlbumService>(MockBehavior.Strict);
            _controller = new AlbumsController(_mockService.Object);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 1 — Успішне отримання альбому
        // Перевірка: метод викликано рівно 1 раз
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task GetAlbum_ExistingId_CallsServiceOnce_ReturnsOk()
        {
            
            var expectedAlbum = new Album { Id = 1, Title = "Dark Side", ArtistName = "Pink Floyd", ReleaseYear = 1973 };
            _mockService
                .Setup(s => s.GetAlbumByIdAsync(1))
                .ReturnsAsync(expectedAlbum);

            
            var result = await _controller.GetAlbum(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var album = Assert.IsType<Album>(ok.Value);
            Assert.Equal("Dark Side", album.Title);

            _mockService.Verify(s => s.GetAlbumByIdAsync(1), Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 2 — Альбом не знайдено
        // Перевірка: метод повертає null → контролер повертає NotFound
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task GetAlbum_NotFound_ReturnsNotFound_ServiceCalledOnce()
        {
            _mockService
                .Setup(s => s.GetAlbumByIdAsync(999))
                .ReturnsAsync((Album?)null);

            var result = await _controller.GetAlbum(999);

            
            Assert.IsType<NotFoundResult>(result.Result);
            _mockService.Verify(s => s.GetAlbumByIdAsync(999), Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 3 — Мок генерує виключення
        // Завдання: один зі сценаріїв де мок кидає виключення
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task PostAlbum_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            _mockService
                .Setup(s => s.CreateAlbumAsync(It.IsAny<AlbumDto>(), null))
                .ThrowsAsync(new ArgumentException("Зображення занадто велике. Максимальний розмір: 5 МБ."));

            var dto = new AlbumDto { Title = "Test", ArtistName = "Artist", ReleaseYear = 2020 };

            var result = await _controller.PostAlbum(dto, null);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("5 МБ", bad.Value.ToString());

            _mockService.Verify(s => s.CreateAlbumAsync(It.IsAny<AlbumDto>(), null), Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 4 — Співставлення параметрів (Argument Matching)
        // Завдання: задання складної поведінки через It.Is<T>()
        // Мок повертає різний результат залежно від значення параметру
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task RateAlbum_ArgumentMatching_DifferentBehaviorPerRating()
        {
            
            _mockService
                .Setup(s => s.RateAlbumAsync(It.IsAny<int>(), It.Is<int>(r => r >= 4)))
                .ReturnsAsync(new FavoriteAlbum { Id = 1, AlbumId = 10, UserId = 5, Rating = 5 });

            _mockService
                .Setup(s => s.RateAlbumAsync(It.IsAny<int>(), It.Is<int>(r => r < 4)))
                .ReturnsAsync(new FavoriteAlbum { Id = 1, AlbumId = 10, UserId = 5, Rating = 2 });

            var result1 = await _controller.RateAlbum(1, 5);
            var result2 = await _controller.RateAlbum(1, 2);

            var ok1 = Assert.IsType<OkObjectResult>(result1);
            var ok2 = Assert.IsType<OkObjectResult>(result2);
            Assert.NotEqual(ok1.Value, ok2.Value);

            _mockService.Verify(s => s.RateAlbumAsync(It.IsAny<int>(), It.Is<int>(r => r >= 4)), Times.Once);
            _mockService.Verify(s => s.RateAlbumAsync(It.IsAny<int>(), It.Is<int>(r => r < 4)), Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 5 — Різні відповіді для кожного наступного виклику
        // Завдання: SetupSequence — кожен виклик повертає інше значення
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task GetAlbum_SequentialCalls_ReturnsDifferentResults()
        {
            _mockService
                .SetupSequence(s => s.GetAlbumByIdAsync(42))
                .ReturnsAsync(new Album { Id = 42, Title = "First Call Album", ArtistName = "Artist", ReleaseYear = 2020 })
                .ReturnsAsync((Album?)null);

            
            var result1 = await _controller.GetAlbum(42);
            var result2 = await _controller.GetAlbum(42);

            Assert.IsType<OkObjectResult>(result1.Result);
            Assert.IsType<NotFoundResult>(result2.Result);

            _mockService.Verify(s => s.GetAlbumByIdAsync(42), Times.Exactly(2));
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 6 — Перевірка порядку викликів
        // Завдання: методи мають викликатись у певному порядку
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task AddToFavorites_ThenRemove_VerifyCallOrder()
        {
            // Arrange
            var callOrder = new List<string>();

            _mockService
                .Setup(s => s.AddToFavoritesAsync(10, 1))
                .Callback(() => callOrder.Add("Add"))
                .ReturnsAsync(new FavoriteAlbum { AlbumId = 10, UserId = 1, Rating = 0 });

            _mockService
                .Setup(s => s.RemoveFromFavoritesAsync(10, 1))
                .Callback(() => callOrder.Add("Remove"))
                .ReturnsAsync(true);

            await _controller.AddToFavorites(10, 1);
            await _controller.RemoveFromFavorites(10, 1);

            Assert.Equal(2, callOrder.Count);
            Assert.Equal("Add", callOrder[0]);
            Assert.Equal("Remove", callOrder[1]);

            _mockService.Verify(s => s.AddToFavoritesAsync(10, 1), Times.Once);
            _mockService.Verify(s => s.RemoveFromFavoritesAsync(10, 1), Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 7 — Обробка виключення InvalidOperationException
        // Мок кидає виключення коли альбом вже в улюблених
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task AddToFavorites_AlreadyExists_MockThrowsException_ReturnsConflict()
        {
            _mockService
                .Setup(s => s.AddToFavoritesAsync(5, 1))
                .ThrowsAsync(new InvalidOperationException("Альбом уже в улюблених."));

            
            var result = await _controller.AddToFavorites(5, 1);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("улюблених", conflict.Value.ToString());

            _mockService.Verify(s => s.AddToFavoritesAsync(5, 1), Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 8 — Пошук з argument matching на рядок
        // It.Is<string>() перевіряє що запит не порожній
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task SearchAlbums_NonEmptyQuery_UsesArgumentMatching()
        {
            _mockService
                .Setup(s => s.SearchAlbumsAsync(It.Is<string>(q => !string.IsNullOrEmpty(q))))
                .ReturnsAsync(new List<Album>
                {
                    new Album { Id = 1, Title = "Rock Album", ArtistName = "Band", ReleaseYear = 2020 }
                });

            var result = await _controller.SearchAlbums("Rock");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(ok.Value);
            Assert.Single(albums);
            Assert.Contains(albums, a => a.Title == "Rock Album");

            _mockService.Verify(
                s => s.SearchAlbumsAsync(It.Is<string>(q => !string.IsNullOrEmpty(q))),
                Times.Once);
        }

        // ══════════════════════════════════════════════════════════
        // СЦЕНАРІЙ 9 — DeleteAlbum: перевірка що метод не викликається двічі
        // ══════════════════════════════════════════════════════════
        [Fact]
        public async Task DeleteAlbum_CalledOnce_NeverCalledTwice()
        {
            _mockService
                .Setup(s => s.DeleteAlbumAsync(7))
                .ReturnsAsync(true);

            var result = await _controller.DeleteAlbum(7);

            Assert.IsType<NoContentResult>(result);

            _mockService.Verify(s => s.DeleteAlbumAsync(7), Times.Once);
            _mockService.Verify(s => s.DeleteAlbumAsync(7), Times.AtMostOnce);
        }
    }
}