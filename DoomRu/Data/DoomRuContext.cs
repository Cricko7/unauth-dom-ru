using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoomRu.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DoomRu.Data
{
    public class DoomRuContext : IdentityDbContext<IdentityUser>
    {
        public DoomRuContext (DbContextOptions<DoomRuContext> options)
            : base(options)
        {
        }

        //public DbSet<DoomRu.Models.User> User { get; set; } = default!;
    }
}
