using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DataLayer.Course;
using DataLayer.Entities;
using DataLayer.Entities.Banner;
using DataLayer.Entities.Bloger;
using DataLayer.Entities.Forum;
using DataLayer.Entities.Order;
using DataLayer.Entities.Permission;
using DataLayer.Entities.User;
using DataLayer.Entities.Course;

namespace DataLayer.Context;

public class AsanwebContext : DbContext
{
    public AsanwebContext(DbContextOptions<AsanwebContext> options) : base(options)
    {
        
    }


    #region Table

    public DbSet<Users> Users { get; set; }
    public DbSet<Roles> Roles { get; set; }
    public DbSet<UsersRole> UsersRoles { get; set; }
    public DbSet<WalletType> WalletTypes { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<RolePermission> RolePermission { get; set; }
    public DbSet<Permission> Permission { get; set; }
    public DbSet<CourseGroup> CourseGroup { get; set; }
    public DbSet<Entities.Course.Course> Course { get; set; }
    public DbSet<CourseEpisode> CourseEpisode { get; set; }
    public DbSet<CourseLevel> CourseLevels { get; set; }
    public DbSet<CourseStatus> CourseStatus { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetails> OrderDetails { get; set; }
    public DbSet<UserCourse> UserCourse { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<UserDiscount> UserDiscounts { get; set; }
    public DbSet<CourseComment> CourseComment { get; set; }
    public DbSet<ContactUs> ContactUs { get; set; }
    public DbSet<Blog> Blog { get; set; }
    public DbSet<CommentBlog> CommentBlog { get; set; }
    public DbSet<SubscribeToNewsLetter> SubscribeToNewsLetter { get; set; }
    public DbSet<CourseVote> CourseVote { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<Question> Question { get; set; }
    public DbSet<BannerInformation> BannerInformations { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<UserPlan> UserPlans { get; set; }

    #endregion Table

    #region QueryFilter

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region IsDelete

        modelBuilder.Entity<Users>().HasQueryFilter(u => !u.IsDelete);
        modelBuilder.Entity<Entities.Course.Course>().HasQueryFilter(u => !u.IsDelete);
        modelBuilder.Entity<Roles>().HasQueryFilter(r => !r.IsDelete);
        modelBuilder.Entity<CourseGroup>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<CommentBlog>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<Blog>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<CourseEpisode>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<Order>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<OrderDetails>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<Wallet>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<BannerInformation>().HasQueryFilter(g => !g.IsDelete);
        modelBuilder.Entity<Plan>().HasQueryFilter(g => !g.IsDelete);

        #endregion IsDelete

        #region code for Delete problem FK_Table

        var cascadeFKs = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

        foreach (var fk in cascadeFKs)
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        #endregion code for Delete problem FK_Table

        #region NotMapRelationship

        modelBuilder.Entity<Entities.Course.Course>()
            .HasOne<CourseGroup>(c => c.CourseGroup)
            .WithMany(c => c.Courses)
            .HasForeignKey(c => c.GroupId);

        modelBuilder.Entity<Entities.Course.Course>()
            .HasOne<CourseGroup>(c => c.Group)
            .WithMany(g => g.SubGroup)
            .HasForeignKey(f => f.SubGroup);

        // modelBuilder.Entity<Entities.Course.Course>()
        //     .HasOne<Plan>(s => s.Plans)
        //     .WithMany(s => s.Courses)
        //     .HasForeignKey(s => s.PlanId);

        // modelBuilder.Entity<Entities.Course.Course>()
        // .HasMany<Plan>(s => s.Plans)
        // .WithRequired(s => s)
        // .HasForeignKey(s => s.PlanId);

        #endregion NotMapRelationship

        base.OnModelCreating(modelBuilder);
    }

    #endregion QueryFilter
}