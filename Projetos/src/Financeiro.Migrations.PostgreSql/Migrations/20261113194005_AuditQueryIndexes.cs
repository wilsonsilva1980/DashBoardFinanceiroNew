using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AuditQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_auditoria_tenant_id_espaco_id_action_instant",
                table: "auditoria",
                columns: new[] { "tenant_id", "espaco_id", "action", "instant" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_tenant_id_espaco_id_correlation_id",
                table: "auditoria",
                columns: new[] { "tenant_id", "espaco_id", "correlation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_tenant_id_espaco_id_entity_entity_id_instant",
                table: "auditoria",
                columns: new[] { "tenant_id", "espaco_id", "entity", "entity_id", "instant" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_tenant_id_espaco_id_user_id_instant",
                table: "auditoria",
                columns: new[] { "tenant_id", "espaco_id", "user_id", "instant" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_auditoria_tenant_id_espaco_id_action_instant",
                table: "auditoria");

            migrationBuilder.DropIndex(
                name: "IX_auditoria_tenant_id_espaco_id_correlation_id",
                table: "auditoria");

            migrationBuilder.DropIndex(
                name: "IX_auditoria_tenant_id_espaco_id_entity_entity_id_instant",
                table: "auditoria");

            migrationBuilder.DropIndex(
                name: "IX_auditoria_tenant_id_espaco_id_user_id_instant",
                table: "auditoria");
        }
    }
}
