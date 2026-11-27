using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vanderkeep.Dominio.Entidades;
using Vanderkeep.Infraestrutura.Identity;

namespace Vanderkeep.Infraestrutura.Persistencia.Configuracoes;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.UserId).IsRequired();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(Category.MaxTitleLength);

        builder.Property(c => c.NormalizedTitle)
            .IsRequired()
            .HasMaxLength(Category.MaxTitleLength);

        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.UpdatedAtUtc).IsRequired();

        // Restrict delete from AspNetUsers to avoid SQL Server multiple cascade paths (ADR-13).
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.NoteCategories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => new { c.UserId, c.NormalizedTitle })
            .IsUnique()
            .HasDatabaseName("UX_Categories_UserId_NormalizedTitle");
    }
}
