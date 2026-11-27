using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vanderkeep.Dominio.Entidades;
using Vanderkeep.Infraestrutura.Identity;

namespace Vanderkeep.Infraestrutura.Persistencia.Configuracoes;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.UserId).IsRequired();

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(Note.MaxTitleLength);

        builder.Property(n => n.Description)
            .HasMaxLength(Note.MaxDescriptionLength);

        builder.Property(n => n.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(n => n.IsPinned).IsRequired();
        builder.Property(n => n.IsArchived).IsRequired();
        builder.Property(n => n.IsDeleted).IsRequired();
        builder.Property(n => n.DeletedAtUtc);
        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.UpdatedAtUtc).IsRequired();

        // Restrict delete from AspNetUsers to avoid SQL Server multiple cascade paths (ADR-13).
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(n => n.NoteCategories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(n => new { n.UserId, n.IsDeleted, n.IsArchived, n.IsPinned, n.UpdatedAtUtc })
            .HasDatabaseName("IX_Notes_User_State_Pinned_UpdatedAt");

        builder.HasIndex(n => new { n.UserId, n.IsDeleted, n.DeletedAtUtc })
            .HasDatabaseName("IX_Notes_User_Deleted_DeletedAt");
    }
}
