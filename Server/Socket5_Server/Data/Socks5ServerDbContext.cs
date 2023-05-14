using Microsoft.EntityFrameworkCore;
using Socks5_Server.Models;

namespace Socks5_Server.Data
{
    public class Socks5ServerDbContext : DbContext
    {
        public Socks5ServerDbContext(DbContextOptions option) : base(option)
        {
            
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(new User()
            {
                UserName = "Admin",
                Password = "123456",
                ExpireTime = System.DateTime.MaxValue,
                Role = Role.Admin
            });
        }
    }
}
