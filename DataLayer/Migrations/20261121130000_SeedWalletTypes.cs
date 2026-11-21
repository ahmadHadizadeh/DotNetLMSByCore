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
        // Use SQL instead of InsertData so this migration does not depend on a generated
        // target-model designer file. Conflicting pre-existing lookup values fail safely.
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1 FROM [WalletTypes]
                WHERE [TypeId] = 1 AND [TypeTitle] <> N'واریز به حساب'
            )
                THROW 51001, N'WalletTypes.TypeId 1 already exists with a different title.', 1;

            IF NOT EXISTS (SELECT 1 FROM [WalletTypes] WHERE [TypeId] = 1)
                INSERT INTO [WalletTypes] ([TypeId], [TypeTitle])
                VALUES (1, N'واریز به حساب');

            IF EXISTS (
                SELECT 1 FROM [WalletTypes]
                WHERE [TypeId] = 2 AND [TypeTitle] <> N'برداشت از حساب'
            )
                THROW 51002, N'WalletTypes.TypeId 2 already exists with a different title.', 1;

            IF NOT EXISTS (SELECT 1 FROM [WalletTypes] WHERE [TypeId] = 2)
                INSERT INTO [WalletTypes] ([TypeId], [TypeTitle])
                VALUES (2, N'برداشت از حساب');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM [Wallets] WHERE [TypeId] IN (1, 2))
                THROW 51003, N'Wallet transaction types cannot be removed while wallet rows reference them.', 1;

            DELETE FROM [WalletTypes]
            WHERE [TypeId] IN (1, 2);
            """);
    }
}
