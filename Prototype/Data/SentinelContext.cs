using Microsoft.EntityFrameworkCore;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Data;

public class SentinelContext(DbContextOptions<SentinelContext> options) : DbContext(options)
{
    private DbSet<ActiveDirectoryModel> ActiveDirectories { get; set; }
    private DbSet<ApplicationHealthModel> ApplicationHealth { get; set; }
    private DbSet<ApplicationModel> Applications { get; set; }
    private DbSet<AuditLogModel> AuditLogs { get; set; }
    private DbSet<ConnectionCredentialModel> ConnectionCredentials { get; set; }
    private DbSet<DataSourceConnectionModel> DataSourceConnections { get; set; }
    private DbSet<EmployeeModel> Employee { get; set; }
    private DbSet<EmployeePermissionModel> EmployeePermissions { get; set; }
    private DbSet<EmployeeRoleModel> EmployeeRoles { get; set; }
    private DbSet<HumanResourceModel> HumanResource { get; set; }
    private DbSet<UserModel> Users { get; set; }
    private DbSet<UserSessionModel> UserSessions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveDirectoryModel>(entity =>
        {
            entity.Property(a => a.ActiveDirectoryId).HasConversion<Guid>();
        });
        
        modelBuilder.Entity<ApplicationHealthModel>(entity =>
        {
            entity.Property(ah => ah.ApplicationHealthId).HasConversion<Guid>();
        });
        
        modelBuilder.Entity<ApplicationModel>(entity =>
        {
            entity.Property(a => a.ApplicationId).HasConversion<Guid>();
            
            entity.Property(a => a.DataSourceConnectionId).HasConversion<Guid>();
            
            entity.Property(a => a.Status).HasConversion(
                a => a.ToString(),
                a => (StatusEnum)Enum.Parse(typeof(StatusEnum), a));
            
            entity.Property(a => a.ApplicationPermission).HasConversion(
                a => a.ToString(),
                a => (ApplicationPermissionEnum)Enum.Parse(typeof(ApplicationPermissionEnum), a));
        });
        
        modelBuilder.Entity<AuditLogModel>(entity =>
        {
            entity.Property(al => al.AuditLogId).HasConversion<Guid>();
            
            entity.Property(al => al.ActionType).HasConversion(
                al => al.ToString(),
                al => (ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), al));
        });
        
        modelBuilder.Entity<ConnectionCredentialModel>(entity =>
        {
            entity.Property(cc => cc.ConnectionCredentialId).HasConversion<Guid>();
            
            entity.Property(cc => cc.CredentialType).HasConversion(
                u => u.ToString(),
                u => (CredentialTypeEnum)Enum.Parse(typeof(CredentialTypeEnum), u));
        });
        
        modelBuilder.Entity<DataSourceConnectionModel>(entity =>
        {
            entity.Property(dc => dc.DataSourceConnectionId).HasConversion<Guid>();
            
            entity.Property(dc => dc.DataSourceType).HasConversion(
                u => u.ToString(),
                u => (DataSourceTypeEnum)Enum.Parse(typeof(DataSourceTypeEnum), u));
            
            entity.Property(dc => dc.ConnectionCredentialId).HasConversion<Guid>();

        });
        
        modelBuilder.Entity<EmployeeModel>(entity =>
        {
            entity.Property(e => e.EmployeeId).HasConversion<Guid>();
            
            entity.Property(e => e.Status).HasConversion(
                e => e.ToString(),
                e => (StatusEnum)Enum.Parse(typeof(StatusEnum), e));
            
            entity.Property(e => e.EmployeePermissionId).HasConversion<Guid>();
            
            entity.Property(e => e.EmployeeRoleId).HasConversion<Guid>();
            
            entity.Property(e => e.ApplicationId).HasConversion<Guid>();
        });

        modelBuilder.Entity<EmployeePermissionModel>(entity =>
        {
            entity.Property(ep => ep.EmployeePermissionId).HasConversion<Guid>();
            
            entity.Property(ep => ep.Permission).HasConversion(
                ep => ep.ToString(),
                ep => (PermissionEnum)Enum.Parse(typeof(PermissionEnum), ep));
        });

        modelBuilder.Entity<EmployeeRoleModel>(entity =>
        {
            entity.Property(er => er.EmployeeRoleId).HasConversion<Guid>();
        });
        
        modelBuilder.Entity<HumanResourceModel>(entity =>
        {
            entity.Property(hr => hr.HumanResourceId).HasConversion<Guid>();
            
            entity.Property(hr => hr.Status).HasConversion(
                hr => hr.ToString(),
                hr => (StatusEnum)Enum.Parse(typeof(StatusEnum), hr));
            
            entity.Property(hr => hr.Permission).HasConversion(
                hr => hr.ToString(),
                hr => (PermissionEnum)Enum.Parse(typeof(PermissionEnum), hr));
        });
        
        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.Property(u => u.UserId).HasConversion<Guid>();
            
            entity.Property(u => u.UserSessionId).HasConversion<Guid>();

            entity.Property(u => u.Permission).HasConversion(
                u => u.ToString(),
                u => (PermissionEnum)Enum.Parse(typeof(PermissionEnum), u));
            
            entity.Property(u => u.Status).HasConversion(
                u => u.ToString(),
                u => (StatusEnum)Enum.Parse(typeof(StatusEnum), u));
        });
        
        modelBuilder.Entity<UserSessionModel>(entity =>
        {
            entity.Property(us => us.UserSessionId).HasConversion<Guid>();

            entity.Property(us => us.ActionTypeEnum).HasConversion(
                us => us.ToString(),
                us => (ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), us));
        });
    }
}