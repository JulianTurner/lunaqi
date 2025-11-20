using LunaQi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LunaQi.Api.Data;

public class LunaQiDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPhase> UserPhases => Set<UserPhase>();
    public DbSet<PhaseDefinition> PhaseDefinitions => Set<PhaseDefinition>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public string DbPath { get; }
    
    public LunaQiDbContext(DbContextOptions<LunaQiDbContext> options)
        : base(options)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "lunaqi.db");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(
            entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired();
                entity.Property(x => x.PasswordHash).IsRequired();
            });
        
        modelBuilder.Entity<PhaseDefinition>(
            entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
            });
        
        modelBuilder.Entity<UserPhase>(
            entity =>
            {
                entity.HasKey(e => new { e.UserId, e.PhaseDefinitionId });
                entity.Property(e => e.IsEnabled).IsRequired();
                entity.HasOne(up => up.User)
                    .WithMany(u => u.UserPhases)
                    .HasForeignKey(up => up.UserId);
                entity.HasOne(up => up.PhaseDefinition)
                    .WithMany()
                    .HasForeignKey(up => up.PhaseDefinitionId);
            });
        
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.Token }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.Property(x => x.Token).IsRequired();
            entity.Property(x => x.ExpiresAt).IsRequired();
        });
    }
}