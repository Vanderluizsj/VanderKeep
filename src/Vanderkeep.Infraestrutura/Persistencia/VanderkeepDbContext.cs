using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vanderkeep.Dominio.Entidades;
using Vanderkeep.Infraestrutura.Identity;

namespace Vanderkeep.Infraestrutura.Persistencia;

public class VanderkeepDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public VanderkeepDbContext(DbContextOptions<VanderkeepDbContext> options)
        : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<NoteCategory> NoteCategories => Set<NoteCategory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(VanderkeepDbContext).Assembly);
    }
}
