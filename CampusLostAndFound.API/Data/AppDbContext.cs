using CampusLostAndFound.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
    }
}