using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("posts");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.AuthorId).HasColumnName("author_id").IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        b.Property(e => e.Content).HasColumnName("content").IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("draft");
        b.Property(e => e.PublishedAt).HasColumnName("published_at");
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.Status);
        b.HasIndex(e => e.AuthorId);

        b.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId);
        b.HasMany(e => e.Attachments).WithOne(a => a.Post).HasForeignKey(a => a.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> b)
    {
        b.ToTable("post_comments");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.PostId).HasColumnName("post_id").IsRequired();
        b.Property(e => e.AuthorMemberId).HasColumnName("author_member_id").IsRequired();
        b.Property(e => e.Content).HasColumnName("content").IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => e.PostId);
        b.HasOne(e => e.Post).WithMany().HasForeignKey(e => e.PostId);
        b.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorMemberId);
    }
}

public class PostReactionConfiguration : IEntityTypeConfiguration<PostReaction>
{
    public void Configure(EntityTypeBuilder<PostReaction> b)
    {
        b.ToTable("post_reactions");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.PostId).HasColumnName("post_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.ReactionType).HasColumnName("reaction_type").HasMaxLength(50).IsRequired();
        b.Property(e => e.CreatedAt).HasColumnName("created_at");

        b.HasIndex(e => new { e.PostId, e.MemberId, e.ReactionType }).IsUnique();
        b.HasOne(e => e.Post).WithMany().HasForeignKey(e => e.PostId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}
