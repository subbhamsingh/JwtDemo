using JwtDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtDemo.Data
{
    public class AppDbContext:DbContext
    {
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
