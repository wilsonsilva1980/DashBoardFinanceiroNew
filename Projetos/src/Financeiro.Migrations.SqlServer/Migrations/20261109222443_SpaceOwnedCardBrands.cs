using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Migrations.SqlServer.Migrations;

public partial class SpaceOwnedCardBrands : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_cartoes_bandeiras_brand_id", table: "cartoes");
        migrationBuilder.DropIndex(name: "IX_cartoes_brand_id", table: "cartoes");
        // Preserve the original global table, including brands without a known owner.
        migrationBuilder.CreateTable(
            name: "bandeiras_espaco",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                espaco_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                active = table.Column<bool>(type: "bit", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bandeiras_espaco", x => x.id);
                table.UniqueConstraint("AK_bandeiras_espaco_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                table.CheckConstraint("CK_ScopedCardBrand_version", "\"version\" > 0");
                table.ForeignKey("FK_bandeiras_espaco_AspNetUsers_created_by", x => x.created_by, "AspNetUsers", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_bandeiras_espaco_espacos_tenant_id_espaco_id", x => new { x.tenant_id, x.espaco_id }, "espacos", new[] { "tenant_id", "id" }, onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_bandeiras_espaco_created_by", "bandeiras_espaco", "created_by");
        migrationBuilder.CreateIndex("IX_bandeiras_espaco_tenant_id_espaco_id_name", "bandeiras_espaco", new[] { "tenant_id", "espaco_id", "name" }, unique: true);
        migrationBuilder.Sql("""
INSERT INTO bandeiras_espaco (id, tenant_id, espaco_id, name, active, version, created_at, updated_at, created_by)
SELECT NEWID(), refs.tenant_id, refs.espaco_id, legacy.name, CAST(1 AS bit), legacy.version, legacy.created_at, legacy.updated_at, legacy.created_by
FROM (SELECT DISTINCT tenant_id, espaco_id, brand_id FROM cartoes WHERE brand_id IS NOT NULL) refs
JOIN bandeiras legacy ON legacy.id = refs.brand_id;
UPDATE card SET brand_id = owned.id
FROM cartoes card
JOIN bandeiras legacy ON card.brand_id = legacy.id
JOIN bandeiras_espaco owned ON owned.tenant_id = card.tenant_id AND owned.espaco_id = card.espaco_id AND owned.name = legacy.name;
""");
        migrationBuilder.CreateIndex("IX_cartoes_tenant_id_espaco_id_brand_id", "cartoes", new[] { "tenant_id", "espaco_id", "brand_id" });
        migrationBuilder.AddForeignKey("FK_cartoes_bandeiras_espaco_tenant_id_espaco_id_brand_id", "cartoes", new[] { "tenant_id", "espaco_id", "brand_id" }, "bandeiras_espaco", principalColumns: new[] { "tenant_id", "espaco_id", "id" }, onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Space-owned brands cannot be collapsed without losing independent edits. Use a forward migration.");
}
