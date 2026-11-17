using DataLayer.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

[DbContext(typeof(AsanwebContext))]
[Migration("20261117170000_AddAuthenticationRateLimitEntries")]
public partial class AddAuthenticationRateLimitEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuthenticationRateLimitEntries",
            columns: table => new
            {
                Scope = table.Column<byte>(type: "tinyint", nullable: false),
                KeyHash = table.Column<string>(
                    type: "char(64)",
                    unicode: false,
                    fixedLength: true,
                    maxLength: 64,
                    nullable: false),
                FailedAttempts = table.Column<int>(type: "int", nullable: false),
                WindowStartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                BlockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ConsecutiveLockouts = table.Column<int>(type: "int", nullable: false),
                LastAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_AuthenticationRateLimitEntries",
                    entry => new { entry.Scope, entry.KeyHash });
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuthenticationRateLimitEntries_LastAttemptUtc",
            table: "AuthenticationRateLimitEntries",
            column: "LastAttemptUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuthenticationRateLimitEntries");
    }
}
