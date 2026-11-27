using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VKeeper.Dominio.Entidades;
using VKeeper.Infraestrutura.Identity;

namespace VKeeper.Infraestrutura.Persistencia;

public class VKeeperDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public VKeeperDbContext(DbContextOptions<VKeeperDbContext> options)
        : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<NoteCategory> NoteCategories => Set<NoteCategory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(VKeeperDbContext).Assembly);
    }
}
