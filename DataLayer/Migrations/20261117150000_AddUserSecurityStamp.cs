using DataLayer.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

[DbContext(typeof(AsanwebContext))]
[Migration("20261117150000_AddUserSecurityStamp")]
public partial class AddUserSecurityStamp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SecurityStamp",
            table: "Users",
            type: "uniqueidentifier",
            nullable: false,
            defaultValueSql: "NEWID()");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SecurityStamp",
            table: "Users");
    }
}