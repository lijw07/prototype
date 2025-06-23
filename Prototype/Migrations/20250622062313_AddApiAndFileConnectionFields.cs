using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prototype.Migrations
{
    /// <inheritdoc />
    public partial class AddApiAndFileConnectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiEndpoint",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "ApplicationConnections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationUrl",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BearerToken",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "ApplicationConnections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomProperties",
                table: "ApplicationConnections",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Delimiter",
                table: "ApplicationConnections",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Encoding",
                table: "ApplicationConnections",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileFormat",
                table: "ApplicationConnections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasHeader",
                table: "ApplicationConnections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Headers",
                table: "ApplicationConnections",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ApplicationConnections",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBody",
                table: "ApplicationConnections",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "ApplicationConnections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenUrl",
                table: "ApplicationConnections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiEndpoint",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "AuthorizationUrl",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "BearerToken",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "CustomProperties",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "Delimiter",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "Encoding",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "FileFormat",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "HasHeader",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "Headers",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "RequestBody",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ApplicationConnections");

            migrationBuilder.DropColumn(
                name: "TokenUrl",
                table: "ApplicationConnections");
        }
    }
}
