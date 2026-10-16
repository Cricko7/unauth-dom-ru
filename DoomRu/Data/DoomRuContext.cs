using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DoomRu.Data
{
    public class DoomRuContext : IdentityDbContext<IdentityUser>
    {
        public DoomRuContext(DbContextOptions<DoomRuContext> options)
            : base(options) { }
    }
}
