using Microsoft.EntityFrameworkCore;
using MyfirstApp.Models;

namespace MyfirstApp.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options): base(options)
        {

        }
   
        public DbSet<Product> Products { get; set; }
    }
}
