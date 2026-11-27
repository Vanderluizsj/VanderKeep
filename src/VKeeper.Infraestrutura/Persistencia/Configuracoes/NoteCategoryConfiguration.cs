using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VKeeper.Dominio.Entidades;

namespace VKeeper.Infraestrutura.Persistencia.Configuracoes;

public sealed class NoteCategoryConfiguration : IEntityTypeConfiguration<NoteCategory>
{
    public void Configure(EntityTypeBuilder<NoteCategory> builder)
    {
        builder.ToTable("NoteCategories");

        builder.HasKey(nc => new { nc.NoteId, nc.CategoryId });

        builder.HasOne(nc => nc.Note)
            .WithMany(n => n.NoteCategories)
            .HasForeignKey(nc => nc.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nc => nc.Category)
            .WithMany(c => c.NoteCategories)
            .HasForeignKey(nc => nc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(nc => new { nc.CategoryId, nc.NoteId })
            .HasDatabaseName("IX_NoteCategories_CategoryId_NoteId");
    }
}
