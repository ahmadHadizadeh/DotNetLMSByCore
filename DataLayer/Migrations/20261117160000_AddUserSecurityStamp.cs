using DataLayer.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

[DbContext(typeof(AsanwebContext))]
[Migration("20261117160000_AddUserSecurityStamp")]
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

        migrationBuilder.DropColumn(
            name: "SessionVersion",
            table: "Users");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SessionVersion",
            table: "Users",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.DropColumn(
            name: "SecurityStamp",
            table: "Users");
    }
}