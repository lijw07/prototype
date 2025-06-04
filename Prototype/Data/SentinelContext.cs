using Microsoft.EntityFrameworkCore;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Data;

public class SentinelContext(DbContextOptions<SentinelContext> options) : DbContext(options)
{
    public DbSet<ActiveDirectoryModel> ActiveDirectories { get; set; }
    public DbSet<ApplicationHealthLogModel> ApplicationApplicationHealths { get; set; }
    public DbSet<ApplicationHealthModel> ApplicationHealth { get; set; }
    public DbSet<ApplicationModel> Applications { get; set; }
    public DbSet<AuditLogModel> AuditLogs { get; set; }
    public DbSet<ConnectionCredentialModel> ConnectionCredentials { get; set; }
    public DbSet<DataSourceConnectionModel> DataSourceConnections { get; set; }
    public DbSet<EmployeeApplicationModel> EmployeeApplications { get; set; }
    public DbSet<EmployeeModel> Employee { get; set; }
    public DbSet<EmployeePermissionModel> EmployeePermissions { get; set; }
    public DbSet<EmployeeRoleModel> EmployeeRoles { get; set; }
    public DbSet<HumanResourceModel> HumanResource { get; set; }
    public DbSet<TemporaryUserModel> TemporaryUser { get; set; }
    public DbSet<UserApplicationModel> UserApplications { get; set; }
    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserSessionModel> UserSessions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region ActiveDirectoryModel

        modelBuilder.Entity<ActiveDirectoryModel>(entity =>
        {
            entity.Property(a => a.ActiveDirectoryId).HasConversion<Guid>();
        });

        #endregion

        #region ApplicationHealthLogModel

        modelBuilder.Entity<ApplicationHealthLogModel>(entity =>
        {
            entity.HasKey(aah => new { aah.ApplicationId, aah.ApplicationHealthId });
            entity.HasOne(aah => aah.Application)
                .WithMany(a => a.ApplicationHealthLog)
                .HasForeignKey(aah => aah.ApplicationId);

            entity.HasOne(aah => aah.ApplicationHealth)
                .WithMany(ah => ah.ApplicationHealthLog)
                .HasForeignKey(aah => aah.ApplicationHealthId);
        });

        #endregion
        
        #region ApplicationHealthModel

        modelBuilder.Entity<ApplicationHealthModel>(entity =>
        {
            entity.Property(ah => ah.ApplicationHealthId).HasConversion<Guid>();
        });

        #endregion

        #region ApplicationModel

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

        #endregion

        #region AuditLogModel

        modelBuilder.Entity<AuditLogModel>(entity =>
        {
            entity.Property(al => al.AuditLogId).HasConversion<Guid>();
            
            entity.Property(al => al.ActionType).HasConversion(
                al => al.ToString(),
                al => (ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), al));
        });

        #endregion

        #region ConnectionCredentialModel

        modelBuilder.Entity<ConnectionCredentialModel>(entity =>
        {
            entity.Property(cc => cc.ConnectionCredentialId).HasConversion<Guid>();
            
            entity.Property(cc => cc.CredentialType).HasConversion(
                u => u.ToString(),
                u => (CredentialTypeEnum)Enum.Parse(typeof(CredentialTypeEnum), u));
        });

        #endregion

        #region MyRegion

        modelBuilder.Entity<DataSourceConnectionModel>(entity =>
        {
            entity.Property(dc => dc.DataSourceConnectionId).HasConversion<Guid>();
            
            entity.Property(dc => dc.DataSourceType).HasConversion(
                u => u.ToString(),
                u => (DataSourceTypeEnum)Enum.Parse(typeof(DataSourceTypeEnum), u));
            
            entity.Property(dc => dc.ConnectionCredentialId).HasConversion<Guid>();

        });

        #endregion

        #region DataSourceConnectionModel

        modelBuilder.Entity<DataSourceConnectionModel>(entity =>
        {
            entity.Property(dc => dc.DataSourceConnectionId).HasConversion<Guid>();
            
            entity.Property(dc => dc.DataSourceType).HasConversion(
                u => u.ToString(),
                u => (DataSourceTypeEnum)Enum.Parse(typeof(DataSourceTypeEnum), u));
            
            entity.Property(dc => dc.ConnectionCredentialId).HasConversion<Guid>();

        });

        #endregion

        #region EmployeeApplicationModel

        modelBuilder.Entity<EmployeeApplicationModel>(entity =>
        {
            entity.HasKey(ea => new { ea.EmployeeId, ea.ApplicationId });
            entity.HasOne(ea => ea.Employee).WithMany(e => e.EmployeeApplications).HasForeignKey(ea => ea.EmployeeId);
            entity.HasOne(ea => ea.Application).WithMany(a => a.EmployeeApplications).HasForeignKey(ea => ea.ApplicationId);
        });

        #endregion
        
        #region EmployeeModel

        modelBuilder.Entity<EmployeeModel>(entity =>
        {
            entity.Property(e => e.EmployeeId).HasConversion<Guid>();
            
            entity.Property(e => e.Status).HasConversion(
                e => e.ToString(),
                e => (StatusEnum)Enum.Parse(typeof(StatusEnum), e));
            
            entity.Property(e => e.EmployeePermissionId).HasConversion<Guid>();
            
            entity.Property(e => e.EmployeeRoleId).HasConversion<Guid>();
        });
        

        #endregion

        #region EmployeePermissionModel

        modelBuilder.Entity<EmployeePermissionModel>(entity =>
        {
            entity.Property(ep => ep.EmployeePermissionId).HasConversion<Guid>();
            
            entity.Property(ep => ep.Permission).HasConversion(
                ep => ep.ToString(),
                ep => (PermissionEnum)Enum.Parse(typeof(PermissionEnum), ep));
        });

        #endregion

        #region EmployeeRoleModel

        modelBuilder.Entity<EmployeeRoleModel>(entity =>
        {
            entity.Property(er => er.EmployeeRoleId).HasConversion<Guid>();
        });

        #endregion

        #region HumanResourceModel

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

        #endregion

        #region TemporaryUserModel

        modelBuilder.Entity<TemporaryUserModel>(entity =>
        {
            entity.Property(tu => tu.TemporaryUserId).HasConversion<Guid>();
            
            entity.Property(tu => tu.UserSessionId).HasConversion<Guid>();
        });

        #endregion

        #region UserApplicationModel

        modelBuilder.Entity<UserApplicationModel>(entity =>
        {
            entity.HasKey(ua => new { ua.UserId, ua.ApplicationId });
        });

        #endregion

        #region UserApplicationModel

        modelBuilder.Entity<UserApplicationModel>(entity =>
        {
            entity.HasKey(ua => new { ua.UserId, ua.ApplicationId });
            entity.HasOne(ua => ua.User).WithMany(u => UserApplications).HasForeignKey(ua => ua.UserId);
            entity.HasOne(ua => ua.Application).WithMany(a => UserApplications).HasForeignKey(ua => ua.ApplicationId);
        });

        #endregion

        #region UserModel

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

        #endregion

        #region UserSessionModel

        modelBuilder.Entity<UserSessionModel>(entity =>
        {
            entity.Property(us => us.UserSessionId).HasConversion<Guid>();

            entity.Property(us => us.ActionType).HasConversion(
                us => us.ToString(),
                us => (ActionTypeEnum)Enum.Parse(typeof(ActionTypeEnum), us));
        });

        #endregion

        //TODO: MOCK DATA IS ONLY FOR TESTING PURPOSES REMOVE ONCE TICKET IS COMPLETE
        //EnsureMockDataAsync();
    }

    // Call this method to insert mock data into SQL Server
    public async void EnsureMockDataAsync()
    {
        try
        {
            if (this.Users.Any()) return;

            var cc = new ConnectionCredentialModel
            {
                ConnectionCredentialId = Guid.NewGuid(),
                CredentialType = CredentialTypeEnum.UsernamePassword,
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };

            var dsc = new DataSourceConnectionModel
            {
                DataSourceConnectionId = Guid.NewGuid(),
                DataSourceName = "SQL Server",
                DataSourceType = DataSourceTypeEnum.SqlServer,
                ConnectionCredentialId = cc.ConnectionCredentialId,
                ConnectionCredential = cc,
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };
            
            var ep = new EmployeePermissionModel
            {
                EmployeePermissionId = Guid.NewGuid(),
                Permission = PermissionEnum.PLATFORM_ADMIN,
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };

            var er = new EmployeeRoleModel
            {
                EmployeeRoleId = Guid.NewGuid(),
                JobTitle = "test",
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };

            var e = new EmployeeModel
            {
                EmployeeId = Guid.NewGuid(),
                Username = "test",
                PasswordHash = "$2a$12$ufJcchkuPcSL9102MEUXD.TY/xCawv.qJ5uOixBFNv2PG8OEqg2Gi",
                FirstName = "test",
                LastName = "test",
                Email = "test@test.com",
                PhoneNumber = "301-203-8888",
                Manager = "test",
                Department = "test",
                Location = "test",
                JobTitle = "test",
                EmployeePermissionId = ep.EmployeePermissionId,
                EmployeePermission = ep,
                EmployeeRoleId = er.EmployeeRoleId,
                EmployeeRole = er,
                EmployeeApplications = new List<EmployeeApplicationModel>(),
                Status = StatusEnum.ACTIVE,
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };
            
            // ApplicationModel
            var app1 = new ApplicationModel
            {
                ApplicationId = Guid.NewGuid(),
                Name = "DemoApp",
                DataSourceConnectionId = dsc.DataSourceConnectionId,
                DataSourceConnection = dsc,
                Status = StatusEnum.ACTIVE,
                ApplicationPermission = ApplicationPermissionEnum.Admin,
                EmployeeApplications = new List<EmployeeApplicationModel>(),
                ApplicationHealthLog = new List<ApplicationHealthLogModel>(),
                UserApplications = new List<UserApplicationModel>(),
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };

            // ActiveDirectoryModel (stub)
            var ad1 = new ActiveDirectoryModel
            {
                ActiveDirectoryId = Guid.NewGuid(),
                Email = "test@test.com",
                Username = "test",
                Password = "$2a$12$ufJcchkuPcSL9102MEUXD.TY/xCawv.qJ5uOixBFNv2PG8OEqg2Gi",
                Status = StatusEnum.ACTIVE,
                User = new List<UserModel>()
            };

            // AuditLogModel (stub)
            var audit1 = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.Create,
                User = new List<UserModel>(),
                ResourceAffected = "test",
                Timestamp = DateTime.Now.Date
            };

            // UserSessionModel (stub)
            var session1 = new UserSessionModel
            {
                UserSessionId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.Create,
                ResourceAffected = "test",
                CreatedAt = DateTime.Now.Date
            };

            // HumanResourceModel (stub)
            var hr1 = new HumanResourceModel
            {
                HumanResourceId = Guid.NewGuid(),
                Firstname = "test",
                Lastname = "test",
                Email = "test@test.com",
                PhoneNumber = "301-203-8308",
                Manager = "test",
                Department = "test",
                Status = StatusEnum.ACTIVE,
                Permission = PermissionEnum.PLATFORM_ADMIN,
                User = new List<UserModel>(),
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };

            // UserModel
            var user1 = new UserModel
            {
                UserId = Guid.NewGuid(),
                Username = "jdoe",
                PasswordHash = "hashed-pass",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@demo.com",
                PhoneNumber = "555-1234",
                Manager = "Jane Boss",
                Department = "Engineering",
                Location = "HQ",
                JobTitle = "Software Engineer",
                UserApplication = new List<UserApplicationModel>(),
                ActiveDirectoryId = ad1.ActiveDirectoryId,
                ActiveDirectory = ad1,
                AuditLogId = audit1.AuditLogId,
                AuditLog = audit1,
                UserSessionId = session1.UserSessionId,
                UserSession = session1,
                HumanResourceId = hr1.HumanResourceId,
                HumanResource = hr1,
                Permission = PermissionEnum.PLATFORM_ADMIN,
                Status = StatusEnum.ACTIVE,
                CreatedAt = DateTime.Now.Date,
                UpdatedAt = DateTime.Now.Date
            };
            await ActiveDirectories.AddAsync(ad1);
            await AuditLogs.AddAsync(audit1);
            await ConnectionCredentials.AddAsync(cc);
            await DataSourceConnections.AddAsync(dsc);
            await EmployeePermissions.AddAsync(ep);
            await EmployeeRoles.AddAsync(er);
            await Employee.AddAsync(e);
            await Applications.AddAsync(app1);
            await UserSessions.AddAsync(session1);
            await HumanResource.AddAsync(hr1);
            await Users.AddAsync(user1);

            await SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

}