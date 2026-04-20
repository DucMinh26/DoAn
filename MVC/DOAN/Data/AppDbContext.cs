using Microsoft.EntityFrameworkCore;
using DOAN.Models;
using DOAN.Models.Entites;

namespace DOAN.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }

        public DbSet<User> Users { get; set; }

    }
}