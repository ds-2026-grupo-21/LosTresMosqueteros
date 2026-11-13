using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Preferences;
using LosTresMosqueteros.Productos;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
namespace LosTresMosqueteros.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ConnectionStringName("Default")]
public class LosTresMosqueterosDbContext :
    AbpDbContext<LosTresMosqueterosDbContext>,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */
    public DbSet<Producto> Productos { get; set; }
    public DbSet<PantryItem> PantryItems { get; set; }
    public DbSet<ExpirationWarning> ExpirationWarnings { get; set; }
    public DbSet<UserPreference> UserPreferences { get; set; }
    public DbSet<WatchedItem> WatchedItems { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext 
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext .
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    #endregion

    public LosTresMosqueterosDbContext(DbContextOptions<LosTresMosqueterosDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureBlobStoring();

        /* Configure your own tables/entities inside here */

        builder.Entity<Producto>(b =>
        {
            b.ToTable(LosTresMosqueterosConsts.DbTablePrefix + "Productos", LosTresMosqueterosConsts.DbSchema);
            b.ConfigureByConvention(); //auto configure for the base class props

            b.Property(x => x.CodigoBarras)
                .IsRequired()
                .HasMaxLength(ProductoConsts.MaxCodigoBarrasLength);

            b.Property(x => x.Nombre)
                .IsRequired(false);

            b.Property(x => x.Ingredientes)
                .IsRequired(false);

            b.Property(x => x.Alergenos)
                .IsRequired(false);

            b.HasIndex(x => x.CodigoBarras).IsUnique();
        });

        builder.Entity<PantryItem>(b =>
        {
            b.ToTable(LosTresMosqueterosConsts.DbTablePrefix + "PantryItems", LosTresMosqueterosConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.OwnerId).IsRequired();
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.Unit).HasMaxLength(50);

            b.HasIndex(x => x.OwnerId);
        });

        builder.Entity<ExpirationWarning>(b =>
        {
            b.ToTable(LosTresMosqueterosConsts.DbTablePrefix + "ExpirationWarnings", LosTresMosqueterosConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PantryItemId).IsRequired();
            b.Property(x => x.Type).IsRequired();
            b.Property(x => x.ExpirationDate).IsRequired();
            b.Property(x => x.DaysRemaining).IsRequired();
            b.Property(x => x.IsActive).IsRequired();

            b.HasOne<PantryItem>()
                .WithMany()
                .HasForeignKey(x => x.PantryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Clave lógica: una sola advertencia por ítem y tipo
            b.HasIndex(x => new { x.PantryItemId, x.Type }).IsUnique();
        });

        builder.Entity<UserPreference>(b =>
        {
            b.ToTable(LosTresMosqueterosConsts.DbTablePrefix + "UserPreferences", LosTresMosqueterosConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.UserId).IsRequired();
            b.HasIndex(x => x.UserId).IsUnique(); // una sola fila de preferencias por usuario

            b.HasMany(x => x.WatchedItems)
                .WithOne()
                .HasForeignKey(x => x.UserPreferenceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WatchedItem>(b =>
        {
            b.ToTable(LosTresMosqueterosConsts.DbTablePrefix + "WatchedItems", LosTresMosqueterosConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(PreferenceConsts.MaxNameLength);
            b.Property(x => x.NormalizedName).IsRequired().HasMaxLength(PreferenceConsts.MaxNameLength);

            b.HasIndex(x => new { x.UserPreferenceId, x.NormalizedName }).IsUnique();
        });
    }
}
