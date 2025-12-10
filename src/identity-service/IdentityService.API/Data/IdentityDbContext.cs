using Microsoft.EntityFrameworkCore;
using IdentityService.API.Entities;

namespace IdentityService.API.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<User> Users => Set<User>();
}
