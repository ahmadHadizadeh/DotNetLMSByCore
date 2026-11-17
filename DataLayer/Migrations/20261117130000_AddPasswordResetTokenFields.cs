using DataLayer.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

[DbContext(typeof(AsanwebContext))]
[Migration("20261117130000_AddPasswordResetTokenFields")]
public partial class AddPasswordResetTokenFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PasswordResetTokenHash",
            table: "Users",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PasswordResetTokenExpiresAt",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_PasswordResetTokenHash",
            table: "Users",
            column: "PasswordResetTokenHash",
            unique: true,
            filter: "[PasswordResetTokenHash] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_PasswordResetTokenHash",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "PasswordResetTokenExpiresAt",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "PasswordResetTokenHash",
            table: "Users");
    }
}
