using Microsoft.EntityFrameworkCore;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Data;

public class SentinelContext(DbContextOptions<SentinelContext> options) : DbContext(options)
{
    public DbSet<ApplicationConnectionModel> ApplicationConnections { get; set; }
    public DbSet<ApplicationLogModel> ApplicationLogs { get; set; }
    public DbSet<ApplicationModel> Applications { get; set; }
    public DbSet<AuditLogModel> AuditLogs { get; set; }
    public DbSet<AuthenticationModel> Authentications { get; set; }
    public DbSet<DataSourceModel> DataSources { get; set; }
    public DbSet<HumanResourceModel> HumanResources { get; set; }
    public DbSet<TemporaryUserModel> TemporaryUsers { get; set; }
    public DbSet<UserActivityLogModel> UserActivityLogs { get; set; }
    public DbSet<UserApplicationModel> UserApplications { get; set; }
    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserPermissionModel> UserPermissions { get; set; }
    public DbSet<UserRecoveryRequestModel> UserRecoveryRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region ApplicationLogModel

        modelBuilder.Entity<ApplicationLogModel>()
            .Property(al => al.ApplicationActionType)
            .HasConversion(
                aat => aat.ToString(),
                aat => (ApplicationActionTypeEnum)System.Enum.Parse(typeof(ApplicationActionTypeEnum), aat)
            );

        #endregion

        #region AuditLogModel

        modelBuilder.Entity<AuditLogModel>()
            .Property(al => al.ActionType)
            .HasConversion(at => at.ToString(), at => (ActionTypeEnum)System.Enum.Parse(typeof(ActionTypeEnum), at));

        #endregion

        #region AuthenticationModel

        modelBuilder.Entity<AuthenticationModel>()
            .Property(a => a.Authentication)
            .HasConversion(a => a.ToString(), a => (AuthenticationTypeEnum)System.Enum.Parse(typeof(AuthenticationTypeEnum), a));

        #endregion

        #region HumanResourceModel

        modelBuilder.Entity<HumanResourceModel>()
            .HasOne(hr => hr.User)
            .WithOne()
            .HasForeignKey<HumanResourceModel>(hr => hr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HumanResourceModel>()
            .Property(hr => hr.Status)
            .HasConversion(s => s.ToString(), s => (StatusEnum)System.Enum.Parse(typeof(StatusEnum), s));

        #endregion

        #region UserActivityLogModel

        modelBuilder.Entity<UserActivityLogModel>()
            .Property(ual => ual.ActionType)
            .HasConversion(at => at.ToString(), at => (ActionTypeEnum)System.Enum.Parse(typeof(ActionTypeEnum), at));

        #endregion

        #region UserApplicationModel

        modelBuilder.Entity<UserApplicationModel>()
            .HasKey(ua => new { ua.UserId, ua.ApplicationId });
        
        modelBuilder.Entity<UserApplicationModel>()
            .HasOne(ua => ua.Application)
            .WithMany()
            .HasForeignKey(ua => ua.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        #endregion

        #region UserRecoveryRequestModel

        modelBuilder.Entity<UserRecoveryRequestModel>()
            .Property(urr => urr.RecoveryType)
            .HasConversion(
                ust => ust.ToString(),
                ust => (UserRecoveryTypeEnum)System.Enum.Parse(typeof(UserRecoveryTypeEnum), ust)
            );

        modelBuilder.Entity<UserRecoveryRequestModel>()
            .Property(urr => urr.IsUsed)
            .HasDefaultValue(false);

        #endregion
    }
}
