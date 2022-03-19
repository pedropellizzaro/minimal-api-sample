using Microsoft.EntityFrameworkCore;
using MinimalApiSample.Models;

namespace MinimalApiSample.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        public DbSet<Supplier> Suppliers => Set<Supplier>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Supplier>()
                .HasKey(x => x.Id);

            builder.Entity<Supplier>()
                .Property(x => x.Name)
                .IsRequired()
                .HasColumnType("varchar(200)");

            builder.Entity<Supplier>()
                .Property(x => x.Document)
                .IsRequired()
                .HasColumnType("varchar(14)");

            builder.Entity<Supplier>()
                .ToTable("Suppliers");

            base.OnModelCreating(builder);
        }
    }
}