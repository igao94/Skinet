using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            new IdentityRole
            {
                Id = "932a6a0a-2d1d-4f00-9f59-dc8823ab8290",
                Name = "Admin",
                NormalizedName = "ADMIN"
            },

            new IdentityRole
            {
                Id = "59b90238-21e9-4fc7-9d0b-7a64146d8685",
                Name = "Customer",
                NormalizedName = "CUSTOMER"
            });
    }
}
