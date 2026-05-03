namespace WebApplicationMusic.Controllers
{
    public class AlbumDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int ReleaseYear { get; set; }
        public int ArtistId { get; set; }
        public required string ArtistName { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
