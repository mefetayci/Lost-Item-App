namespace CampusLostAndFound.API.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public string? FinderName { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsHandedOver { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // YENİ: Kör Bilgi (Blind Match) Sistemi İçin Gizli Alanlar
        public string? SecretQuestion { get; set; }
        public string? SecretAnswer { get; set; }
    }
}