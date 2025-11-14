using Microsoft.EntityFrameworkCore;
using Suparco.Api.Models;

namespace Suparco.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<UserModel> Users { get; set; }
    }
}
