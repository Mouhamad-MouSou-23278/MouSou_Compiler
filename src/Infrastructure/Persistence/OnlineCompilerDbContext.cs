using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Infrastructure.Persistence;

public class OnlineCompilerDbContext : DbContext, IApplicationDbContext
{
    public OnlineCompilerDbContext(DbContextOptions<OnlineCompilerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<AIJob> AIJobs => Set<AIJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Language).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Projects)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).HasMaxLength(500).IsRequired();
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Files)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ProjectId, e.Path }).IsUnique();
        });

        modelBuilder.Entity<Execution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Language).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Executions)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Executions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasOne(e => e.Execution)
                .WithMany(e => e.AIJobs)
                .HasForeignKey(e => e.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
