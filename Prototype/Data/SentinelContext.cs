using Microsoft.EntityFrameworkCore;
using Prototype.Models;

namespace Prototype.Data;

public class SentinelContext(DbContextOptions<SentinelContext> options) : DbContext(options)
{
     public DbSet<ActiveDirectoryModel> ActiveDirectory { get; set; }
     public DbSet<ApplicationModel> Application { get; set; }
     public DbSet<ApplicationPermissionsModel> ApplicationPermissions { get; set; }
     public DbSet<DataSourceConnectionModel> DataSourceConnection { get; set; }
     public DbSet<EmployeeModel> Employee { get; set; }
     public DbSet<HumanResourcesModel> HumanResources { get; set; }
     public DbSet<UserModel> Users { get; set; }
}