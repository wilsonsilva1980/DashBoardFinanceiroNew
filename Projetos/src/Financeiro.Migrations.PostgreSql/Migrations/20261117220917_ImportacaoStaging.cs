using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class ImportacaoStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "importacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    parser_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    statistics_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacoes", x => x.id);
                    table.UniqueConstraint("AK_importacoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ImportBatch_state_enum", "\"state\" IN ('Received','Analyzed','AwaitingReview','Confirmed','Failed','Cancelled')");
                    table.CheckConstraint("CK_ImportBatch_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_importacoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_importacoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "anotacoes_origem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sheet_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cell_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    source_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anotacoes_origem", x => x.id);
                    table.UniqueConstraint("AK_anotacoes_origem_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ImportAnnotation_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_anotacoes_origem_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_origem_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_origem_importacoes_tenant_id_espaco_id_import_bat~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.import_batch_id },
                        principalTable: "importacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_origem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sheet_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_sheet_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    start_column = table.Column<int>(type: "integer", nullable: true),
                    cell_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    raw_content_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    derived_values_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    issue_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    issue_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    prevailing_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_origem", x => x.id);
                    table.UniqueConstraint("AK_registros_origem_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ImportSourceRecord_classification_enum", "\"classification\" IN ('Record','Header','Section','Subtotal','Total','Model')");
                    table.CheckConstraint("CK_ImportSourceRecord_decision_enum", "\"decision\" IN ('Pending','Approved','Rejected','Ignored')");
                    table.CheckConstraint("CK_ImportSourceRecord_severity_enum", "\"severity\" IN ('Error','Warning','Info')");
                    table.CheckConstraint("CK_ImportSourceRecord_source_type_enum", "\"source_type\" IN ('Annual','Installments','Anticipation','Backup','Catalog','Recurrence','Travel','Scenario')");
                    table.CheckConstraint("CK_ImportSourceRecord_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_registros_origem_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_registros_origem_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_registros_origem_importacoes_tenant_id_espaco_id_import_bat~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.import_batch_id },
                        principalTable: "importacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vinculos_origem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    occurrence_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculos_origem", x => x.id);
                    table.UniqueConstraint("AK_vinculos_origem_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ImportLink_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_vinculos_origem_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vinculos_origem_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vinculos_origem_importacoes_tenant_id_espaco_id_import_batc~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.import_batch_id },
                        principalTable: "importacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vinculos_origem_registros_origem_tenant_id_espaco_id_source~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.source_record_id },
                        principalTable: "registros_origem",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_origem_created_by",
                table: "anotacoes_origem",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_origem_import_batch_id_sheet_name_cell_reference",
                table: "anotacoes_origem",
                columns: new[] { "import_batch_id", "sheet_name", "cell_reference" });

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_origem_tenant_id_espaco_id_import_batch_id",
                table: "anotacoes_origem",
                columns: new[] { "tenant_id", "espaco_id", "import_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_created_by",
                table: "importacoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_tenant_id_espaco_id_content_hash",
                table: "importacoes",
                columns: new[] { "tenant_id", "espaco_id", "content_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_tenant_id_espaco_id_state_created_at",
                table: "importacoes",
                columns: new[] { "tenant_id", "espaco_id", "state", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_registros_origem_created_by",
                table: "registros_origem",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_registros_origem_import_batch_id_classification",
                table: "registros_origem",
                columns: new[] { "import_batch_id", "classification" });

            migrationBuilder.CreateIndex(
                name: "IX_registros_origem_import_batch_id_decision",
                table: "registros_origem",
                columns: new[] { "import_batch_id", "decision" });

            migrationBuilder.CreateIndex(
                name: "IX_registros_origem_import_batch_id_severity",
                table: "registros_origem",
                columns: new[] { "import_batch_id", "severity" });

            migrationBuilder.CreateIndex(
                name: "IX_registros_origem_tenant_id_espaco_id_import_batch_id",
                table: "registros_origem",
                columns: new[] { "tenant_id", "espaco_id", "import_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_vinculos_origem_created_by",
                table: "vinculos_origem",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_vinculos_origem_import_batch_id_source_record_id_entity_typ~",
                table: "vinculos_origem",
                columns: new[] { "import_batch_id", "source_record_id", "entity_type", "entity_id", "role", "occurrence_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vinculos_origem_tenant_id_espaco_id_import_batch_id",
                table: "vinculos_origem",
                columns: new[] { "tenant_id", "espaco_id", "import_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_vinculos_origem_tenant_id_espaco_id_source_record_id",
                table: "vinculos_origem",
                columns: new[] { "tenant_id", "espaco_id", "source_record_id" });

            migrationBuilder.Sql("""
INSERT INTO permissoes (id, key, description, module, version, created_at, updated_at, created_by)
SELECT '8063f50e-1596-f24f-0a1f-bfec81be69a2'::uuid, 'importacoes.visualizar', 'importacoes.visualizar', 'importacoes', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL
WHERE NOT EXISTS (SELECT 1 FROM permissoes WHERE key = 'importacoes.visualizar');
INSERT INTO permissoes (id, key, description, module, version, created_at, updated_at, created_by)
SELECT '61f101e3-3421-08c8-0723-6dc695d34c9c'::uuid, 'importacoes.executar', 'importacoes.executar', 'importacoes', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL
WHERE NOT EXISTS (SELECT 1 FROM permissoes WHERE key = 'importacoes.executar');
INSERT INTO perfil_permissoes (id, role_id, permission_id, version, created_at, updated_at, created_by, tenant_id, espaco_id)
SELECT gen_random_uuid(), role.id, permission.id, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, role.tenant_id, role.espaco_id
FROM perfis AS role
JOIN permissoes AS permission ON permission.key IN ('importacoes.visualizar', 'importacoes.executar')
WHERE role.name IN ('Administrador', 'Gestor financeiro')
  AND NOT EXISTS (
      SELECT 1 FROM perfil_permissoes AS existing
      WHERE existing.tenant_id = role.tenant_id
        AND existing.espaco_id = role.espaco_id
        AND existing.role_id = role.id
        AND existing.permission_id = permission.id);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anotacoes_origem");

            migrationBuilder.DropTable(
                name: "vinculos_origem");

            migrationBuilder.DropTable(
                name: "registros_origem");

            migrationBuilder.DropTable(
                name: "importacoes");

            migrationBuilder.Sql("""
DELETE FROM perfil_permissoes
WHERE permission_id IN (SELECT id FROM permissoes WHERE key IN ('importacoes.visualizar', 'importacoes.executar'));
DELETE FROM permissoes WHERE key IN ('importacoes.visualizar', 'importacoes.executar');
""");
        }
    }
}
