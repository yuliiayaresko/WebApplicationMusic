using Microsoft.AspNetCore.Http;
using WebApplicationMusic.Controllers;
using WebApplicationMusic.Models;

namespace WebApplicationMusic.Services
{
    public interface IAlbumService
    {
        Task<IEnumerable<Album>> GetAllAlbumsAsync();
        Task<Album?> GetAlbumByIdAsync(int id);
        Task<IEnumerable<Album>> SearchAlbumsAsync(string query);
        Task<IEnumerable<object>> GetFavoritesAsync(int userId);
        Task<IEnumerable<object>> GetFavoritesByUserAsync(int userId);
        Task<Album> CreateAlbumAsync(AlbumDto dto, IFormFile? coverImage);
        Task<bool> UpdateAlbumAsync(int id, AlbumDto dto, IFormFile? coverImage);
        Task<bool> DeleteAlbumAsync(int id);
        Task<FavoriteAlbum?> AddToFavoritesAsync(int albumId, int userId);
        Task<bool> RemoveFromFavoritesAsync(int albumId, int userId);
        Task<FavoriteAlbum?> RateAlbumAsync(int favoriteId, int rating);
    }
}