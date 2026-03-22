using Microsoft.EntityFrameworkCore;
using Moq;
using WebApplicationMusic.Models;

namespace WebApplicationMusic.Tests.Fixtures
{
    public class TestFixture : IDisposable
    {
        // Властивість для отримання опцій контексту
        public DbContextOptions<MusicAPIContext> DbOptions { get; }

        public TestFixture()
        {
            // Налаштовуємо базу в пам'яті
            DbOptions = new DbContextOptionsBuilder<MusicAPIContext>()
                .UseInMemoryDatabase(databaseName: "MusicTestDb_" + Guid.NewGuid().ToString())
                .Options;

            Console.WriteLine("✅ TestFixture ініціалізовано з InMemory DB");
        }

        public void Dispose()
        {
            // Очищення, якщо потрібно
        }
    }
}