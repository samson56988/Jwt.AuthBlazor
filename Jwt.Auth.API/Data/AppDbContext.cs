using Jwt.Auth.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jwt.Auth.API.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContext) : base(dbContext) 
        { 
        
         
        }

        public DbSet<User> User { get; set; }
    }
}
