using Microsoft.EntityFrameworkCore.Migrations;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Financeiro.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FinanceiroDbContext))]
[Migration("20261118143500_ImportSourceRecordJsonPayload")]
public partial class ImportSourceRecordJsonPayload : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "raw_content_json",
            table: "registros_origem",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(8000)",
            oldMaxLength: 8000);

        migrationBuilder.AlterColumn<string>(
            name: "derived_values_json",
            table: "registros_origem",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(8000)",
            oldMaxLength: 8000);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "raw_content_json",
            table: "registros_origem",
            type: "character varying(8000)",
            maxLength: 8000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "derived_values_json",
            table: "registros_origem",
            type: "character varying(8000)",
            maxLength: 8000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");
    }
}
