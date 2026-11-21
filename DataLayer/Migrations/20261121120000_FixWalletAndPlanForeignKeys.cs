using DataLayer.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations;

[DbContext(typeof(AsanwebContext))]
[Migration("20261121120000_FixWalletAndPlanForeignKeys")]
public partial class FixWalletAndPlanForeignKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Plans_Users_UsersUserId",
            table: "Plans");

        migrationBuilder.DropIndex(
            name: "IX_Plans_UsersUserId",
            table: "Plans");

        migrationBuilder.DropColumn(
            name: "UsersUserId",
            table: "Plans");

        migrationBuilder.CreateIndex(
            name: "IX_Plans_UserId",
            table: "Plans",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Plans_Users_UserId",
            table: "Plans",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "UserId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropForeignKey(
            name: "FK_Wallets_WalletTypes_WalletTypesTypeId",
            table: "Wallets");

        migrationBuilder.DropIndex(
            name: "IX_Wallets_WalletTypesTypeId",
            table: "Wallets");

        migrationBuilder.DropColumn(
            name: "WalletTypesTypeId",
            table: "Wallets");

        migrationBuilder.CreateIndex(
            name: "IX_Wallets_TypeId",
            table: "Wallets",
            column: "TypeId");

        migrationBuilder.AddForeignKey(
            name: "FK_Wallets_WalletTypes_TypeId",
            table: "Wallets",
            column: "TypeId",
            principalTable: "WalletTypes",
            principalColumn: "TypeId",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Wallets_WalletTypes_TypeId",
            table: "Wallets");

        migrationBuilder.DropIndex(
            name: "IX_Wallets_TypeId",
            table: "Wallets");

        migrationBuilder.AddColumn<int>(
            name: "WalletTypesTypeId",
            table: "Wallets",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            "UPDATE [Wallets] SET [WalletTypesTypeId] = [TypeId];");

        migrationBuilder.CreateIndex(
            name: "IX_Wallets_WalletTypesTypeId",
            table: "Wallets",
            column: "WalletTypesTypeId");

        migrationBuilder.AddForeignKey(
            name: "FK_Wallets_WalletTypes_WalletTypesTypeId",
            table: "Wallets",
            column: "WalletTypesTypeId",
            principalTable: "WalletTypes",
            principalColumn: "TypeId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropForeignKey(
            name: "FK_Plans_Users_UserId",
            table: "Plans");

        migrationBuilder.DropIndex(
            name: "IX_Plans_UserId",
            table: "Plans");

        migrationBuilder.AddColumn<int>(
            name: "UsersUserId",
            table: "Plans",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            "UPDATE [Plans] SET [UsersUserId] = [UserId];");

        migrationBuilder.CreateIndex(
            name: "IX_Plans_UsersUserId",
            table: "Plans",
            column: "UsersUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Plans_Users_UsersUserId",
            table: "Plans",
            column: "UsersUserId",
            principalTable: "Users",
            principalColumn: "UserId",
            onDelete: ReferentialAction.Restrict);
    }
}
