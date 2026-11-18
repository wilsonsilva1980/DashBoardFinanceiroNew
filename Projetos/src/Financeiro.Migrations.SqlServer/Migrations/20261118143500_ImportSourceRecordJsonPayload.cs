using Microsoft.EntityFrameworkCore.Migrations;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Financeiro.Migrations.SqlServer.Migrations;

[DbContext(typeof(FinanceiroDbContext))]
[Migration("20261118143500_ImportSourceRecordJsonPayload")]
public partial class ImportSourceRecordJsonPayload : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "raw_content_json",
            table: "registros_origem",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldMaxLength: 8000);

        migrationBuilder.AlterColumn<string>(
            name: "derived_values_json",
            table: "registros_origem",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldMaxLength: 8000);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "raw_content_json",
            table: "registros_origem",
            type: "nvarchar(max)",
            maxLength: 8000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "derived_values_json",
            table: "registros_origem",
            type: "nvarchar(max)",
            maxLength: 8000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");
    }
}
