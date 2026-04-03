using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.Identity.CosmosDb.Demo
{
    public class CosmosIdentityDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public CosmosIdentityDbContext(DbContextOptions<CosmosIdentityDbContext> options)
            : base(options)
        {
        }
    }
}
