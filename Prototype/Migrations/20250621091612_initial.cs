using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prototype.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApplicationDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicationDataSourceType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                });

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    DataSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataSourceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Host = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataSourceType = table.Column<int>(type: "int", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.DataSourceId);
                });

            migrationBuilder.CreateTable(
                name: "TemporaryUsers",
                columns: table => new
                {
                    TemporaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryUsers", x => x.TemporaryUserId);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationConnections",
                columns: table => new
                {
                    ApplicationConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Instance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Host = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Port = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AuthenticationType = table.Column<int>(type: "int", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthenticationDatabase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AwsAccessKeyId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AwsSecretAccessKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AwsSessionToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Principal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceRealm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CanonicalizeHostName = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationConnections", x => x.ApplicationConnectionId);
                    table.ForeignKey(
                        name: "FK_ApplicationConnections_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationLogs",
                columns: table => new
                {
                    ApplicationLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationModelApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLogs", x => x.ApplicationLogId);
                    table.ForeignKey(
                        name: "FK_ApplicationLogs_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId");
                    table.ForeignKey(
                        name: "FK_ApplicationLogs_Applications_ApplicationModelApplicationId",
                        column: x => x.ApplicationModelApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId");
                });

            migrationBuilder.CreateTable(
                name: "Authentications",
                columns: table => new
                {
                    AuthenticationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Authentication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthenticationDatabase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AwsAccessKeyId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AwsSecretKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AwsSessionToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Principal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceRealm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanonicalizeHostName = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authentications", x => x.AuthenticationId);
                    table.ForeignKey(
                        name: "FK_Authentications_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "DataSourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HumanResources",
                columns: table => new
                {
                    HumanResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanResources", x => x.HumanResourceId);
                    table.ForeignKey(
                        name: "FK_HumanResources_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    UserActivityLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    DeviceInformation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.UserActivityLogId);
                    table.ForeignKey(
                        name: "FK_UserActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRecoveryRequests",
                columns: table => new
                {
                    UserRecoveryRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RecoveryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecoveryRequests", x => x.UserRecoveryRequestId);
                    table.ForeignKey(
                        name: "FK_UserRecoveryRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserApplications",
                columns: table => new
                {
                    UserApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationConnectionModelApplicationConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicationModelApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserModelUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserApplications", x => x.UserApplicationId);
                    table.ForeignKey(
                        name: "FK_UserApplications_ApplicationConnections_ApplicationConnectionId",
                        column: x => x.ApplicationConnectionId,
                        principalTable: "ApplicationConnections",
                        principalColumn: "ApplicationConnectionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserApplications_ApplicationConnections_ApplicationConnectionModelApplicationConnectionId",
                        column: x => x.ApplicationConnectionModelApplicationConnectionId,
                        principalTable: "ApplicationConnections",
                        principalColumn: "ApplicationConnectionId");
                    table.ForeignKey(
                        name: "FK_UserApplications_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserApplications_Applications_ApplicationModelApplicationId",
                        column: x => x.ApplicationModelApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId");
                    table.ForeignKey(
                        name: "FK_UserApplications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserApplications_Users_UserModelUserId",
                        column: x => x.UserModelUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationConnections_ApplicationId",
                table: "ApplicationConnections",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_ApplicationId",
                table: "ApplicationLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_ApplicationModelApplicationId",
                table: "ApplicationLogs",
                column: "ApplicationModelApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Authentications_DataSourceId",
                table: "Authentications",
                column: "DataSourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HumanResources_UserId",
                table: "HumanResources",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplications_ApplicationConnectionId",
                table: "UserApplications",
                column: "ApplicationConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplications_ApplicationConnectionModelApplicationConnectionId",
                table: "UserApplications",
                column: "ApplicationConnectionModelApplicationConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplications_ApplicationId",
                table: "UserApplications",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplications_ApplicationModelApplicationId",
                table: "UserApplications",
                column: "ApplicationModelApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplications_UserId",
                table: "UserApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplications_UserModelUserId",
                table: "UserApplications",
                column: "UserModelUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRecoveryRequests_UserId",
                table: "UserRecoveryRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationLogs");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Authentications");

            migrationBuilder.DropTable(
                name: "HumanResources");

            migrationBuilder.DropTable(
                name: "TemporaryUsers");

            migrationBuilder.DropTable(
                name: "UserActivityLogs");

            migrationBuilder.DropTable(
                name: "UserApplications");

            migrationBuilder.DropTable(
                name: "UserRecoveryRequests");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "DataSources");

            migrationBuilder.DropTable(
                name: "ApplicationConnections");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Applications");
        }
    }
}
