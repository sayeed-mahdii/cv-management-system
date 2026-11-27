using Microsoft.EntityFrameworkCore;
using Recruitov.Domain.Entities;
using Recruitov.Domain.Enums;

namespace Recruitov.Infrastructure.Data;

/// <summary>
/// AppDbContext maps domain entities to PostgreSQL tables.
/// Preserves exact composite keys, indexes, cascade rules, concurrency tokens, and seed data.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<UserAttributeValue> UserAttributeValues => Set<UserAttributeValue>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    public DbSet<PositionTag> PositionTags => Set<PositionTag>();
    public DbSet<PositionAccessRule> PositionAccessRules => Set<PositionAccessRule>();
    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
    public DbSet<Like> Likes => Set<Like>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Composite Primary Keys
        modelBuilder.Entity<AppUserRole>().HasKey(x => new { x.UserId, x.Role });
        modelBuilder.Entity<UserAttributeValue>().HasKey(x => new { x.UserId, x.AttributeDefinitionId });
        modelBuilder.Entity<ProjectTag>().HasKey(x => new { x.ProjectId, x.TagId });
        modelBuilder.Entity<PositionAttribute>().HasKey(x => new { x.PositionId, x.AttributeDefinitionId });
        modelBuilder.Entity<PositionTag>().HasKey(x => new { x.PositionId, x.TagId });
        modelBuilder.Entity<Like>().HasKey(x => new { x.CvId, x.RecruiterUserId });

        // 2. Optimistic Concurrency Tokens
        modelBuilder.Entity<AppUser>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<AttributeDefinition>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<Position>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<Cv>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<Project>().Property(x => x.Version).IsConcurrencyToken();

        // 3. Unique Constraints & Performance Indexes
        modelBuilder.Entity<Cv>().HasIndex(x => new { x.UserId, x.PositionId }).IsUnique();
        modelBuilder.Entity<AttributeDefinition>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<AttributeDefinition>().HasIndex(x => x.CategoryId);
        modelBuilder.Entity<AttributeDefinition>().HasIndex(x => x.LastUsedAt);
        modelBuilder.Entity<AttributeOption>().HasIndex(x => new { x.AttributeDefinitionId, x.Label }).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<Position>().HasIndex(x => x.UpdatedAt);
        modelBuilder.Entity<Cv>().HasIndex(x => new { x.PositionId, x.State });
        modelBuilder.Entity<Cv>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<DiscussionPost>().HasIndex(x => new { x.PositionId, x.Id });
        modelBuilder.Entity<Project>().HasIndex(x => new { x.UserId, x.StartDate });

        // 4. Cascade & Restrict Rules
        modelBuilder.Entity<AppUser>().HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AppUser>().HasMany(x => x.AttributeValues).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AppUser>().HasMany(x => x.Projects).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AppUser>().HasMany(x => x.Cvs).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AppUser>().HasMany(x => x.DiscussionPosts).WithOne(x => x.AuthorUser).HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AppUser>().HasMany(x => x.Likes).WithOne(x => x.RecruiterUser).HasForeignKey(x => x.RecruiterUserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttributeDefinition>().HasMany(x => x.Options).WithOne(x => x.AttributeDefinition).HasForeignKey(x => x.AttributeDefinitionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttributeDefinition>().HasMany(x => x.Values).WithOne(x => x.AttributeDefinition).HasForeignKey(x => x.AttributeDefinitionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttributeDefinition>().HasMany(x => x.PositionAttributes).WithOne(x => x.AttributeDefinition).HasForeignKey(x => x.AttributeDefinitionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttributeDefinition>().HasMany(x => x.PositionAccessRules).WithOne(x => x.AttributeDefinition).HasForeignKey(x => x.AttributeDefinitionId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>().HasMany(x => x.Attributes).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Position>().HasMany(x => x.PositionAttributes).WithOne(x => x.Position).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Position>().HasMany(x => x.PositionTags).WithOne(x => x.Position).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Position>().HasMany(x => x.PositionAccessRules).WithOne(x => x.Position).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Position>().HasMany(x => x.DiscussionPosts).WithOne(x => x.Position).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Position>().HasMany(x => x.Cvs).WithOne(x => x.Position).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cv>().HasMany(x => x.Likes).WithOne(x => x.Cv).HasForeignKey(x => x.CvId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>().HasMany(x => x.ProjectTags).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Tag>().HasMany(x => x.ProjectTags).WithOne(x => x.Tag).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Tag>().HasMany(x => x.PositionTags).WithOne(x => x.Tag).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);

        // 5. Seed Data
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Certification" },
            new Category { Id = 2, Name = "Domain Knowledge" },
            new Category { Id = 3, Name = "Soft Skills" }
        );

        modelBuilder.Entity<AttributeDefinition>().HasData(
            new AttributeDefinition { Id = 1, Name = "First Name", NormalizedName = "first name", Description = "Candidate's first name", DataType = DataType.String, CategoryId = 3, IsBuiltIn = true, Version = 1 },
            new AttributeDefinition { Id = 2, Name = "Last Name", NormalizedName = "last name", Description = "Candidate's last name", DataType = DataType.String, CategoryId = 3, IsBuiltIn = true, Version = 1 },
            new AttributeDefinition { Id = 3, Name = "Location", NormalizedName = "location", Description = "City, Country", DataType = DataType.String, CategoryId = 2, IsBuiltIn = true, Version = 1 },
            new AttributeDefinition { Id = 4, Name = "Personal Photo", NormalizedName = "personal photo", Description = "Profile headshot image", DataType = DataType.Image, CategoryId = 3, IsBuiltIn = true, Version = 1 }
        );
    }
}
