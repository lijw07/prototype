using Microsoft.EntityFrameworkCore;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Data;

public class SentinelContext(DbContextOptions<SentinelContext> options) : DbContext(options)
{
    public DbSet<ApplicationConnectionModel> ApplicationConnections { get; set; }
    public DbSet<ApplicationModel> Applications { get; set; }
    public DbSet<AuditLogModel> AuditLogs { get; set; }
    public DbSet<AuthenticationModel> Authentications { get; set; }
    public DbSet<DataSourceModel> DataSources { get; set; }
    public DbSet<HumanResourceModel> HumanResources { get; set; }
    public DbSet<PermissionModel> Permissions { get; set; }
    public DbSet<TemporaryUserModel> TemporaryUsers { get; set; }
    public DbSet<UserActivityLogModel> UserActivityLogs { get; set; }
    public DbSet<UserApplicationModel> UserApplications { get; set; }
    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserRecoveryRequestModel> UserRecoveryRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region ApplicationConnectionModel

        modelBuilder.Entity<ApplicationConnectionModel>()
            .HasOne(ac => ac.Application)
            .WithOne(a => a.ApplicationConnections)
            .HasForeignKey<ApplicationConnectionModel>(ac => ac.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ApplicationConnectionModel>()
            .Property(ac => ac.Status)
            .HasConversion(s => s.ToString(), s => (StatusEnum) Enum.Parse(typeof(StatusEnum), s));
        
        #endregion
        
        #region ApplicationModel
        
        modelBuilder.Entity<ApplicationModel>()
            .HasOne(a => a.Permission)
            .WithMany()
            .HasForeignKey(a => a.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        #endregion
        
        #region AuditLogModel
        
        modelBuilder.Entity<AuditLogModel>()
            .HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<AuditLogModel>()
            .Property(al => al.ActionType)
            .HasConversion(at => at.ToString(), at => (ActionTypeEnum) Enum.Parse(typeof(ActionTypeEnum), at));
        
        #endregion

        #region AuthenticationModel

        modelBuilder.Entity<AuthenticationModel>()
            .Property(a => a.Authentication)
            .HasConversion(a => a.ToString(), a => (AuthenticationTypeEnum) Enum.Parse(typeof(AuthenticationTypeEnum), a));

        modelBuilder.Entity<AuthenticationModel>()
            .HasOne(a => a.DataSource)
            .WithOne(ds => ds.Authentication)
            .HasForeignKey<AuthenticationModel>(a => a.DataSourceId);

        #endregion

        #region DataSourceModel

        modelBuilder.Entity<DataSourceModel>()
            .HasOne(ds => ds.Authentication)
            .WithOne(a => a.DataSource);

        #endregion
        
        #region HumanResourceModel
        
        modelBuilder.Entity<HumanResourceModel>()
            .HasOne(hr => hr.User)
            .WithOne()
            .HasForeignKey<HumanResourceModel>(hr => hr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<HumanResourceModel>()
            .Property(hr => hr.JobTitle)
            .HasConversion(jt => jt.ToString(), jt => (JobPositionEnum) Enum.Parse(typeof(JobPositionEnum), jt));
        
        modelBuilder.Entity<HumanResourceModel>()
            .Property(hr => hr.Department)
            .HasConversion(d => d.ToString(), d => (DepartmentEnum) Enum.Parse(typeof(DepartmentEnum), d));
        
        modelBuilder.Entity<HumanResourceModel>()
            .Property(hr => hr.Status)
            .HasConversion(s => s.ToString(), s => (StatusEnum) Enum.Parse(typeof(StatusEnum), s));
        
        #endregion
        
        #region UserActivityLogModel
        
        modelBuilder.Entity<UserActivityLogModel>()
            .HasOne(ual => ual.User)
            .WithMany(u => u.UserActivityLogs)
            .HasForeignKey(ual => ual.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<UserActivityLogModel>()
            .Property(ual => ual.ActionType)
            .HasConversion(at => at.ToString(), at => (ActionTypeEnum) Enum.Parse(typeof(ActionTypeEnum), at));
        
        #endregion

        #region UserApplicationModel

        modelBuilder.Entity<UserApplicationModel>()
            .HasKey(ua => new { ua.UserId, ua.ApplicationId });

        #endregion
        
        #region UserRecoveryRequestModel
        
        modelBuilder.Entity<UserRecoveryRequestModel>()
            .HasOne(urr => urr.User)
            .WithMany(u => u.UserRecoveryRequests)
            .HasForeignKey(urr => urr.UserId);
        
        modelBuilder.Entity<UserRecoveryRequestModel>()
            .Property(urr => urr.UserRecoveryType)
            .HasConversion(ust => ust.ToString(), ust => (UserRecoveryTypeEnum) Enum.Parse(typeof(UserRecoveryTypeEnum), ust));
        
        #endregion
    }
}