using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vanderkeep.Infraestrutura.Identity;

namespace Vanderkeep.Infraestrutura.Persistencia.Configuracoes;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
    }
}
