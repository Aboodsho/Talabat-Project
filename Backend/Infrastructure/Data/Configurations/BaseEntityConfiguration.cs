using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{
    internal class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired(false);
        }
    }
}

// when entity applied this use  base.Configure(builder);
// and CategoryConfiguration : BaseEntityConfiguration<Category>
// not CategoryConfiguration : IEntityTypeConfiguration<Category>

//so i make override of baseentity configure and use base.configure that call the configure method of base
//and return to run the code in override configure method

// fe b2a 7al => ( template method ) and ( global config )
// eb2a shofhom law 3ayez bas da for big base entity that has many feilds
