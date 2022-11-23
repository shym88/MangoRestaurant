using Microsoft.EntityFrameworkCore;
using Mango.Services.OrderAPI.Models;

namespace Mango.Services.OrderAPI.DbContexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) :base(options)    
        {

        }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }


    }
}
