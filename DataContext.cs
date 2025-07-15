using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrintO.Models;
using PrintO.Models.Integrations;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;

namespace PrintO;

public class DataContext : IdentityDbContext<User, UserRole, int>
{
    public DbSet<Store> stores { get; set; }
    public DbSet<Product> products { get; set; }
    public DbSet<Models.File> files { get; set; }
    public DbSet<ImageReference> images { get; set; }
    public DbSet<InvitationToken> invitationTokens { get; set; }
    public DbSet<Note> notes { get; set; }
    public DbSet<FileTag> tags { get; set; }

    public DbSet<FigurineReference> figurines { get; set; }
    public DbSet<FigurineVariation> figurineVariations { get; set; }

    public DbSet<OzonIntegrationTask> ozonIntegrationTasks { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Ignore<IdentityUserLogin<int>>();
        builder.Ignore<IdentityUserToken<int>>();
        builder.Entity<IdentityUserRole<int>>().HasKey(r => r.UserId);

        builder.Entity<User>()
            .HasOne(u => u.selectedStore)
            .WithMany()
            .HasForeignKey(u => u.selectedStoreId);
    }
}