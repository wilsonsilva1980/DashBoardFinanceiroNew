using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AccessAuditIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sessoes_usuario_user_id",
                table: "sessoes_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_usuario_user_id_expires_at",
                table: "sessoes_usuario",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_tenant_id_espaco_id_instant",
                table: "auditoria",
                columns: new[] { "tenant_id", "espaco_id", "instant" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sessoes_usuario_user_id_expires_at",
                table: "sessoes_usuario");

            migrationBuilder.DropIndex(
                name: "IX_auditoria_tenant_id_espaco_id_instant",
                table: "auditoria");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_usuario_user_id",
                table: "sessoes_usuario",
                column: "user_id");
        }
    }
}
