using DataLayer.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

[DbContext(typeof(AsanwebContext))]
[Migration("20261121130000_SeedWalletTypes")]
public partial class SeedWalletTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "WalletTypes",
            columns: new[] { "TypeId", "TypeTitle" },
            values: new object[,]
            {
                { 1, "واریز به حساب" },
                { 2, "برداشت از حساب" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "WalletTypes",
            keyColumn: "TypeId",
            keyValues: new object[] { 1, 2 });
    }
}
