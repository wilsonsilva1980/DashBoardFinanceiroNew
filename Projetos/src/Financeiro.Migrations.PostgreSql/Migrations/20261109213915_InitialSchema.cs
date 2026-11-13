using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financeiro.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    claim_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    claim_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bandeiras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bandeiras", x => x.id);
                    table.CheckConstraint("CK_CardBrand_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_bandeiras_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permissoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    module = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissoes", x => x.id);
                    table.CheckConstraint("CK_Permission_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_permissoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    public_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.CheckConstraint("CK_tenant_status", "\"status\" IN ('Active','Suspended','Closed')");
                    table.CheckConstraint("CK_Tenant_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_tenants_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "espacos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_espacos", x => x.id);
                    table.UniqueConstraint("AK_espacos_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_FinancialSpace_currency_currency", "\"currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_FinancialSpace_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_espacos_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_espacos_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membros_tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    administrator = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membros_tenant", x => x.id);
                    table.UniqueConstraint("AK_membros_tenant_tenant_id_user_id", x => new { x.tenant_id, x.user_id });
                    table.CheckConstraint("CK_TenantMember_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_membros_tenant_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membros_tenant_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membros_tenant_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instant = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changes_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.id);
                    table.UniqueConstraint("AK_auditoria_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_AuditRecord_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_auditoria_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auditoria_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_auditoria_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.UniqueConstraint("AK_categorias_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Category_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_categorias_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categorias_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cotacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    destination_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    rate_date = table.Column<DateOnly>(type: "date", nullable: true),
                    date_context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rate = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cotacoes", x => x.id);
                    table.UniqueConstraint("AK_cotacoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ExchangeRate_destination_currency_currency", "\"destination_currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_ExchangeRate_rate_amount", "\"rate\" > 0");
                    table.CheckConstraint("CK_ExchangeRate_source_currency_currency", "\"source_currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_ExchangeRate_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_cotacoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cotacoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idempotencia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotencia", x => x.id);
                    table.UniqueConstraint("AK_idempotencia_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_IdempotencyRecord_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_idempotencia_AspNetUsers_actor_id",
                        column: x => x.actor_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idempotencia_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idempotencia_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "instituicoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instituicoes", x => x.id);
                    table.UniqueConstraint("AK_instituicoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Institution_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_instituicoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_instituicoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_operacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_onboarding_operacoes", x => x.id);
                    table.CheckConstraint("CK_OnboardingRecord_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_onboarding_operacoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_onboarding_operacoes_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_onboarding_operacoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    @protected = table.Column<bool>(name: "protected", type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfis", x => x.id);
                    table.UniqueConstraint("AK_perfis_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_SpaceRole_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_perfis_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_perfis_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pessoas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pessoas", x => x.id);
                    table.UniqueConstraint("AK_pessoas_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Person_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_pessoas_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pessoas_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preferencias_espaco",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recurrence_horizon = table.Column<int>(type: "integer", nullable: false),
                    invalid_day_rule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preferencias_espaco", x => x.id);
                    table.UniqueConstraint("AK_preferencias_espaco_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_SpacePreferences_recurrence_horizon", "\"recurrence_horizon\" > 0");
                    table.CheckConstraint("CK_SpacePreferences_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_preferencias_espaco_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_preferencias_espaco_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessoes_usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_access_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    security_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    active_space_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessoes_usuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessoes_usuario_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sessoes_usuario_espacos_active_tenant_id_active_space_id",
                        columns: x => new { x.active_tenant_id, x.active_space_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "viagens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viagens", x => x.id);
                    table.UniqueConstraint("AK_viagens_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_TravelBudget_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_viagens_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_viagens_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membros_espaco",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    access_version = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membros_espaco", x => x.id);
                    table.UniqueConstraint("AK_membros_espaco_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.UniqueConstraint("AK_membros_espaco_tenant_id_espaco_id_user_id", x => new { x.tenant_id, x.espaco_id, x.user_id });
                    table.CheckConstraint("CK_SpaceMember_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_membros_espaco_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membros_espaco_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membros_espaco_membros_tenant_tenant_id_user_id",
                        columns: x => new { x.tenant_id, x.user_id },
                        principalTable: "membros_tenant",
                        principalColumns: new[] { "tenant_id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfil_permissoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil_permissoes", x => x.id);
                    table.UniqueConstraint("AK_perfil_permissoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_RolePermission_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_perfil_permissoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_perfil_permissoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_perfil_permissoes_perfis_tenant_id_espaco_id_role_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.role_id },
                        principalTable: "perfis",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_perfil_permissoes_permissoes_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cartoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    holder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    due_day = table.Column<int>(type: "integer", nullable: true),
                    closing_day = table.Column<int>(type: "integer", nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    cash_limit = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    foreign_cash_limit = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    pix_limit = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    bill_limit = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartoes", x => x.id);
                    table.UniqueConstraint("AK_cartoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_CreditCard_bill_limit_amount", "\"bill_limit\" >= 0");
                    table.CheckConstraint("CK_CreditCard_cash_limit_amount", "\"cash_limit\" >= 0");
                    table.CheckConstraint("CK_CreditCard_closing_day", "\"closing_day\" BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_CreditCard_credit_limit_amount", "\"credit_limit\" >= 0");
                    table.CheckConstraint("CK_CreditCard_due_day", "\"due_day\" BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_CreditCard_foreign_cash_limit_amount", "\"foreign_cash_limit\" >= 0");
                    table.CheckConstraint("CK_CreditCard_pix_limit_amount", "\"pix_limit\" >= 0");
                    table.CheckConstraint("CK_CreditCard_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_cartoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cartoes_bandeiras_brand_id",
                        column: x => x.brand_id,
                        principalTable: "bandeiras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cartoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cartoes_instituicoes_tenant_id_espaco_id_institution_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.institution_id },
                        principalTable: "instituicoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cartoes_pessoas_tenant_id_espaco_id_holder_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.holder_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    holder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    agency = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    check_digit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    overdraft_limit = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas", x => x.id);
                    table.UniqueConstraint("AK_contas_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_BankAccount_currency_currency", "\"currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_BankAccount_overdraft_limit_amount", "\"overdraft_limit\" >= 0");
                    table.CheckConstraint("CK_BankAccount_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_contas_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contas_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contas_instituicoes_tenant_id_espaco_id_institution_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.institution_id },
                        principalTable: "instituicoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contas_pessoas_tenant_id_espaco_id_holder_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.holder_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "obrigacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responsible_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    creditor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kind = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_date_context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    due_day = table.Column<int>(type: "integer", nullable: true),
                    installment_count = table.Column<int>(type: "integer", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obrigacoes", x => x.id);
                    table.UniqueConstraint("AK_obrigacoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Obligation_currency_currency", "\"currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_Obligation_due_day", "\"due_day\" BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_Obligation_installment_count", "\"installment_count\" > 0");
                    table.CheckConstraint("CK_Obligation_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_obrigacoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_obrigacoes_categorias_tenant_id_espaco_id_category_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.category_id },
                        principalTable: "categorias",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_obrigacoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_obrigacoes_pessoas_tenant_id_espaco_id_responsible_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.responsible_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membro_perfis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membro_perfis", x => x.id);
                    table.UniqueConstraint("AK_membro_perfis_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_MemberRole_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_membro_perfis_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membro_perfis_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membro_perfis_membros_espaco_tenant_id_espaco_id_user_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.user_id },
                        principalTable: "membros_espaco",
                        principalColumns: new[] { "tenant_id", "espaco_id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_membro_perfis_perfis_tenant_id_espaco_id_role_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.role_id },
                        principalTable: "perfis",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamentos", x => x.id);
                    table.UniqueConstraint("AK_pagamentos_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Payment_amount_amount", "\"amount\" > 0");
                    table.CheckConstraint("CK_Payment_currency_currency", "\"currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_Payment_state_enum", "\"state\" IN ('Draft','Confirmed','Reversed')");
                    table.CheckConstraint("CK_Payment_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_pagamentos_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagamentos_contas_tenant_id_espaco_id_account_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.account_id },
                        principalTable: "contas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagamentos_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagamentos_pessoas_tenant_id_espaco_id_payer_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.payer_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "posicoes_conta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_date = table.Column<DateOnly>(type: "date", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    savings_balance = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    overdraft_used = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posicoes_conta", x => x.id);
                    table.UniqueConstraint("AK_posicoes_conta_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_AccountPosition_overdraft_used_amount", "\"overdraft_used\" >= 0");
                    table.CheckConstraint("CK_AccountPosition_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_posicoes_conta_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_posicoes_conta_contas_tenant_id_espaco_id_account_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.account_id },
                        principalTable: "contas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_posicoes_conta_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cenarios_quitacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    simulation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    proposed_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cenarios_quitacao", x => x.id);
                    table.UniqueConstraint("AK_cenarios_quitacao_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_SettlementScenario_proposed_amount_amount", "\"proposed_amount\" >= 0");
                    table.CheckConstraint("CK_SettlementScenario_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_cenarios_quitacao_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cenarios_quitacao_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cenarios_quitacao_obrigacoes_tenant_id_espaco_id_obligation~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itens_viagem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    travel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responsible_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    state = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    base_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    fees = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_viagem", x => x.id);
                    table.UniqueConstraint("AK_itens_viagem_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_TravelItem_base_amount_amount", "\"base_amount\" >= 0");
                    table.CheckConstraint("CK_TravelItem_currency_currency", "\"currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_TravelItem_fees_amount", "\"fees\" >= 0");
                    table.CheckConstraint("CK_TravelItem_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_itens_viagem_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itens_viagem_categorias_tenant_id_espaco_id_category_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.category_id },
                        principalTable: "categorias",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itens_viagem_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itens_viagem_obrigacoes_tenant_id_espaco_id_obligation_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itens_viagem_pessoas_tenant_id_espaco_id_responsible_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.responsible_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itens_viagem_viagens_tenant_id_espaco_id_travel_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.travel_id },
                        principalTable: "viagens",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recorrencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anchor_date = table.Column<DateOnly>(type: "date", nullable: true),
                    nominal_day = table.Column<int>(type: "integer", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recorrencias", x => x.id);
                    table.UniqueConstraint("AK_recorrencias_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Recurrence_nominal_day", "\"nominal_day\" BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_Recurrence_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_recorrencias_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recorrencias_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recorrencias_obrigacoes_tenant_id_espaco_id_obligation_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "titulos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsible_id = table.Column<Guid>(type: "uuid", nullable: true),
                    card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    installment_number = table.Column<int>(type: "integer", nullable: true),
                    occurrence_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_titulos", x => x.id);
                    table.UniqueConstraint("AK_titulos_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_PayableTitle_installment_number", "\"installment_number\" > 0");
                    table.CheckConstraint("CK_PayableTitle_original_amount_amount", "\"original_amount\" >= 0");
                    table.CheckConstraint("CK_PayableTitle_period", "EXTRACT(DAY FROM \"period\") = 1");
                    table.CheckConstraint("CK_PayableTitle_state_enum", "\"state\" IN ('Draft','Forecast','Effective','Cancelled')");
                    table.CheckConstraint("CK_PayableTitle_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_titulos_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_titulos_cartoes_tenant_id_espaco_id_card_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.card_id },
                        principalTable: "cartoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_titulos_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_titulos_obrigacoes_tenant_id_espaco_id_obligation_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_titulos_pessoas_tenant_id_espaco_id_responsible_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.responsible_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acordos_quitacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_date = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    proposed_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    state = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acordos_quitacao", x => x.id);
                    table.UniqueConstraint("AK_acordos_quitacao_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_SettlementAgreement_proposed_amount_amount", "\"proposed_amount\" >= 0");
                    table.CheckConstraint("CK_SettlementAgreement_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_acordos_quitacao_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acordos_quitacao_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acordos_quitacao_obrigacoes_tenant_id_espaco_id_obligation_~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acordos_quitacao_pagamentos_tenant_id_espaco_id_payment_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.payment_id },
                        principalTable: "pagamentos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_conversoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    mode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reported_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    date_context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_conversoes", x => x.id);
                    table.UniqueConstraint("AK_item_conversoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ItemConversion_destination_currency_currency", "\"destination_currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_ItemConversion_reported_amount_amount", "\"reported_amount\" >= 0");
                    table.CheckConstraint("CK_ItemConversion_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_item_conversoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_conversoes_cotacoes_tenant_id_espaco_id_rate_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.rate_id },
                        principalTable: "cotacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_conversoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_conversoes_itens_viagem_tenant_id_espaco_id_item_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.item_id },
                        principalTable: "itens_viagem",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recorrencia_vigencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    cycle_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    interval_months = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recorrencia_vigencias", x => x.id);
                    table.UniqueConstraint("AK_recorrencia_vigencias_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_recurrence_dates", "\"end_date\" IS NULL OR \"end_date\" >= \"start_date\"");
                    table.CheckConstraint("CK_recurrence_interval", "\"interval_months\" IN (1,3,12)");
                    table.CheckConstraint("CK_RecurrencePrice_currency_currency", "\"currency\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                    table.CheckConstraint("CK_RecurrencePrice_cycle_amount_amount", "\"cycle_amount\" >= 0");
                    table.CheckConstraint("CK_RecurrencePrice_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_recorrencia_vigencias_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recorrencia_vigencias_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recorrencia_vigencias_recorrencias_tenant_id_espaco_id_recu~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.recurrence_id },
                        principalTable: "recorrencias",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alocacoes_pagamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alocacoes_pagamento", x => x.id);
                    table.UniqueConstraint("AK_alocacoes_pagamento_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_PaymentAllocation_amount_amount", "\"amount\" > 0");
                    table.CheckConstraint("CK_PaymentAllocation_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_alocacoes_pagamento_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alocacoes_pagamento_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alocacoes_pagamento_pagamentos_tenant_id_espaco_id_payment_~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.payment_id },
                        principalTable: "pagamentos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alocacoes_pagamento_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "anotacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: true),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    travel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anotacoes", x => x.id);
                    table.UniqueConstraint("AK_anotacoes_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_Annotation_one_target", "(CASE WHEN \"title_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"obligation_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"travel_id\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_Annotation_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_anotacoes_AspNetUsers_author_id",
                        column: x => x.author_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_obrigacoes_tenant_id_espaco_id_obligation_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anotacoes_viagens_tenant_id_espaco_id_travel_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.travel_id },
                        principalTable: "viagens",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "baixas_historicas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: false),
                    justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    previous_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_baixas_historicas", x => x.id);
                    table.UniqueConstraint("AK_baixas_historicas_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_HistoricalSettlement_amount_amount", "\"amount\" >= 0");
                    table.CheckConstraint("CK_HistoricalSettlement_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_baixas_historicas_AspNetUsers_approved_by",
                        column: x => x.approved_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_baixas_historicas_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_baixas_historicas_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_baixas_historicas_pessoas_tenant_id_espaco_id_payer_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.payer_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_baixas_historicas_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cenario_quitacao_itens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nominal_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cenario_quitacao_itens", x => x.id);
                    table.UniqueConstraint("AK_cenario_quitacao_itens_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_ScenarioTitle_discount_amount", "\"discount\" >= 0");
                    table.CheckConstraint("CK_ScenarioTitle_nominal_amount_amount", "\"nominal_amount\" >= 0");
                    table.CheckConstraint("CK_ScenarioTitle_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_cenario_quitacao_itens_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cenario_quitacao_itens_cenarios_quitacao_tenant_id_espaco_i~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.scenario_id },
                        principalTable: "cenarios_quitacao",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cenario_quitacao_itens_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cenario_quitacao_itens_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faturas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    mode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reported_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faturas", x => x.id);
                    table.UniqueConstraint("AK_faturas_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_CardInvoice_period", "EXTRACT(DAY FROM \"period\") = 1");
                    table.CheckConstraint("CK_CardInvoice_reported_amount_amount", "\"reported_amount\" >= 0");
                    table.CheckConstraint("CK_CardInvoice_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_faturas_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_faturas_cartoes_tenant_id_espaco_id_card_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.card_id },
                        principalTable: "cartoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_faturas_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_faturas_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "planejamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planejamentos", x => x.id);
                    table.UniqueConstraint("AK_planejamentos_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_PlanningEntry_amount_amount", "\"amount\" >= 0");
                    table.CheckConstraint("CK_PlanningEntry_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_planejamentos_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_planejamentos_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_planejamentos_pessoas_tenant_id_espaco_id_person_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.person_id },
                        principalTable: "pessoas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_planejamentos_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "referencias_informadas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title_id = table.Column<Guid>(type: "uuid", nullable: true),
                    obligation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    travel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referencias_informadas", x => x.id);
                    table.UniqueConstraint("AK_referencias_informadas_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_InformedReference_one_target", "(CASE WHEN \"title_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"obligation_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"travel_id\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_InformedReference_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_referencias_informadas_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referencias_informadas_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referencias_informadas_obrigacoes_tenant_id_espaco_id_oblig~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referencias_informadas_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referencias_informadas_viagens_tenant_id_espaco_id_travel_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.travel_id },
                        principalTable: "viagens",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acordo_titulos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nominal_amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acordo_titulos", x => x.id);
                    table.UniqueConstraint("AK_acordo_titulos_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_AgreementTitle_discount_amount", "\"discount\" >= 0");
                    table.CheckConstraint("CK_AgreementTitle_nominal_amount_amount", "\"nominal_amount\" >= 0");
                    table.CheckConstraint("CK_AgreementTitle_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_acordo_titulos_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acordo_titulos_acordos_quitacao_tenant_id_espaco_id_agreeme~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.agreement_id },
                        principalTable: "acordos_quitacao",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acordo_titulos_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acordo_titulos_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ajustes_titulo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_titulo", x => x.id);
                    table.UniqueConstraint("AK_ajustes_titulo_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_TitleAdjustment_amount_amount", "\"amount\" >= 0");
                    table.CheckConstraint("CK_TitleAdjustment_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_ajustes_titulo_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_titulo_acordos_quitacao_tenant_id_espaco_id_agreeme~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.agreement_id },
                        principalTable: "acordos_quitacao",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_titulo_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ajustes_titulo_titulos_tenant_id_espaco_id_title_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.title_id },
                        principalTable: "titulos",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fatura_itens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_obligation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    installment_number = table.Column<int>(type: "integer", nullable: true),
                    occurrence_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    espaco_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura_itens", x => x.id);
                    table.UniqueConstraint("AK_fatura_itens_tenant_id_espaco_id_id", x => new { x.tenant_id, x.espaco_id, x.id });
                    table.CheckConstraint("CK_InvoicePurchase_amount_amount", "\"amount\" >= 0");
                    table.CheckConstraint("CK_InvoicePurchase_installment_number", "\"installment_number\" > 0");
                    table.CheckConstraint("CK_InvoicePurchase_version", "\"version\" > 0");
                    table.ForeignKey(
                        name: "FK_fatura_itens_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fatura_itens_espacos_tenant_id_espaco_id",
                        columns: x => new { x.tenant_id, x.espaco_id },
                        principalTable: "espacos",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fatura_itens_faturas_tenant_id_espaco_id_invoice_id",
                        columns: x => new { x.tenant_id, x.espaco_id, x.invoice_id },
                        principalTable: "faturas",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fatura_itens_obrigacoes_tenant_id_espaco_id_purchase_obliga~",
                        columns: x => new { x.tenant_id, x.espaco_id, x.purchase_obligation_id },
                        principalTable: "obrigacoes",
                        principalColumns: new[] { "tenant_id", "espaco_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acordo_titulos_created_by",
                table: "acordo_titulos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_acordo_titulos_tenant_id_espaco_id_agreement_id_title_id",
                table: "acordo_titulos",
                columns: new[] { "tenant_id", "espaco_id", "agreement_id", "title_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acordo_titulos_tenant_id_espaco_id_title_id",
                table: "acordo_titulos",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_acordos_quitacao_created_by",
                table: "acordos_quitacao",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_acordos_quitacao_tenant_id_espaco_id_obligation_id",
                table: "acordos_quitacao",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_acordos_quitacao_tenant_id_espaco_id_payment_id",
                table: "acordos_quitacao",
                columns: new[] { "tenant_id", "espaco_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_titulo_created_by",
                table: "ajustes_titulo",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_titulo_tenant_id_espaco_id_agreement_id",
                table: "ajustes_titulo",
                columns: new[] { "tenant_id", "espaco_id", "agreement_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_titulo_tenant_id_espaco_id_title_id",
                table: "ajustes_titulo",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_alocacoes_pagamento_created_by",
                table: "alocacoes_pagamento",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_alocacoes_pagamento_tenant_id_espaco_id_payment_id_title_id",
                table: "alocacoes_pagamento",
                columns: new[] { "tenant_id", "espaco_id", "payment_id", "title_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alocacoes_pagamento_tenant_id_espaco_id_title_id",
                table: "alocacoes_pagamento",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_author_id",
                table: "anotacoes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_created_by",
                table: "anotacoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_tenant_id_espaco_id_obligation_id",
                table: "anotacoes",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_tenant_id_espaco_id_title_id",
                table: "anotacoes",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_tenant_id_espaco_id_travel_id",
                table: "anotacoes",
                columns: new[] { "tenant_id", "espaco_id", "travel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email",
                unique: true,
                filter: "\"normalized_email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_created_by",
                table: "auditoria",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_user_id",
                table: "auditoria",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_baixas_historicas_approved_by",
                table: "baixas_historicas",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_baixas_historicas_created_by",
                table: "baixas_historicas",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_baixas_historicas_tenant_id_espaco_id_payer_id",
                table: "baixas_historicas",
                columns: new[] { "tenant_id", "espaco_id", "payer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_baixas_historicas_tenant_id_espaco_id_title_id",
                table: "baixas_historicas",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_bandeiras_created_by",
                table: "bandeiras",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_bandeiras_name",
                table: "bandeiras",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cartoes_brand_id",
                table: "cartoes",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_cartoes_created_by",
                table: "cartoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_cartoes_tenant_id_espaco_id_holder_id",
                table: "cartoes",
                columns: new[] { "tenant_id", "espaco_id", "holder_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cartoes_tenant_id_espaco_id_institution_id",
                table: "cartoes",
                columns: new[] { "tenant_id", "espaco_id", "institution_id" });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_created_by",
                table: "categorias",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_cenario_quitacao_itens_created_by",
                table: "cenario_quitacao_itens",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_cenario_quitacao_itens_tenant_id_espaco_id_scenario_id_titl~",
                table: "cenario_quitacao_itens",
                columns: new[] { "tenant_id", "espaco_id", "scenario_id", "title_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cenario_quitacao_itens_tenant_id_espaco_id_title_id",
                table: "cenario_quitacao_itens",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cenarios_quitacao_created_by",
                table: "cenarios_quitacao",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_cenarios_quitacao_tenant_id_espaco_id_obligation_id",
                table: "cenarios_quitacao",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contas_created_by",
                table: "contas",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_contas_tenant_id_espaco_id_holder_id",
                table: "contas",
                columns: new[] { "tenant_id", "espaco_id", "holder_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contas_tenant_id_espaco_id_institution_id",
                table: "contas",
                columns: new[] { "tenant_id", "espaco_id", "institution_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cotacoes_created_by",
                table: "cotacoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_espacos_created_by",
                table: "espacos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_espacos_tenant_id_normalized_name",
                table: "espacos",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_itens_created_by",
                table: "fatura_itens",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_fatura_itens_tenant_id_espaco_id_invoice_id_occurrence_key",
                table: "fatura_itens",
                columns: new[] { "tenant_id", "espaco_id", "invoice_id", "occurrence_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_itens_tenant_id_espaco_id_purchase_obligation_id_occ~",
                table: "fatura_itens",
                columns: new[] { "tenant_id", "espaco_id", "purchase_obligation_id", "occurrence_key" },
                unique: true,
                filter: "\"purchase_obligation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_created_by",
                table: "faturas",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_tenant_id_espaco_id_card_id_period",
                table: "faturas",
                columns: new[] { "tenant_id", "espaco_id", "card_id", "period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_faturas_tenant_id_espaco_id_title_id",
                table: "faturas",
                columns: new[] { "tenant_id", "espaco_id", "title_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotencia_actor_id",
                table: "idempotencia",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_idempotencia_created_by",
                table: "idempotencia",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_idempotencia_tenant_id_espaco_id_actor_id_operation_request~",
                table: "idempotencia",
                columns: new[] { "tenant_id", "espaco_id", "actor_id", "operation", "request_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instituicoes_created_by",
                table: "instituicoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_item_conversoes_created_by",
                table: "item_conversoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_item_conversoes_tenant_id_espaco_id_item_id",
                table: "item_conversoes",
                columns: new[] { "tenant_id", "espaco_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_item_conversoes_tenant_id_espaco_id_rate_id",
                table: "item_conversoes",
                columns: new[] { "tenant_id", "espaco_id", "rate_id" });

            migrationBuilder.CreateIndex(
                name: "IX_itens_viagem_created_by",
                table: "itens_viagem",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_itens_viagem_tenant_id_espaco_id_category_id",
                table: "itens_viagem",
                columns: new[] { "tenant_id", "espaco_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_itens_viagem_tenant_id_espaco_id_obligation_id",
                table: "itens_viagem",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_itens_viagem_tenant_id_espaco_id_responsible_id",
                table: "itens_viagem",
                columns: new[] { "tenant_id", "espaco_id", "responsible_id" });

            migrationBuilder.CreateIndex(
                name: "IX_itens_viagem_tenant_id_espaco_id_travel_id",
                table: "itens_viagem",
                columns: new[] { "tenant_id", "espaco_id", "travel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membro_perfis_created_by",
                table: "membro_perfis",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_membro_perfis_tenant_id_espaco_id_role_id",
                table: "membro_perfis",
                columns: new[] { "tenant_id", "espaco_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membro_perfis_tenant_id_espaco_id_user_id_role_id",
                table: "membro_perfis",
                columns: new[] { "tenant_id", "espaco_id", "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membros_espaco_created_by",
                table: "membros_espaco",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_membros_espaco_tenant_id_espaco_id_user_id",
                table: "membros_espaco",
                columns: new[] { "tenant_id", "espaco_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membros_espaco_tenant_id_user_id",
                table: "membros_espaco",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_membros_tenant_created_by",
                table: "membros_tenant",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_membros_tenant_tenant_id_user_id",
                table: "membros_tenant",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membros_tenant_user_id",
                table: "membros_tenant",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_obrigacoes_created_by",
                table: "obrigacoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_obrigacoes_tenant_id_espaco_id_category_id",
                table: "obrigacoes",
                columns: new[] { "tenant_id", "espaco_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_obrigacoes_tenant_id_espaco_id_responsible_id",
                table: "obrigacoes",
                columns: new[] { "tenant_id", "espaco_id", "responsible_id" });

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_operacoes_created_by",
                table: "onboarding_operacoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_operacoes_tenant_id_espaco_id",
                table: "onboarding_operacoes",
                columns: new[] { "tenant_id", "espaco_id" });

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_operacoes_user_id_request_key",
                table: "onboarding_operacoes",
                columns: new[] { "user_id", "request_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_created_by",
                table: "pagamentos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_tenant_id_espaco_id_account_id",
                table: "pagamentos",
                columns: new[] { "tenant_id", "espaco_id", "account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_tenant_id_espaco_id_idempotency_key",
                table: "pagamentos",
                columns: new[] { "tenant_id", "espaco_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_tenant_id_espaco_id_payer_id",
                table: "pagamentos",
                columns: new[] { "tenant_id", "espaco_id", "payer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_tenant_id_espaco_id_payment_date",
                table: "pagamentos",
                columns: new[] { "tenant_id", "espaco_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "IX_perfil_permissoes_created_by",
                table: "perfil_permissoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_perfil_permissoes_permission_id",
                table: "perfil_permissoes",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_perfil_permissoes_tenant_id_espaco_id_role_id_permission_id",
                table: "perfil_permissoes",
                columns: new[] { "tenant_id", "espaco_id", "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_perfis_created_by",
                table: "perfis",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_perfis_tenant_id_espaco_id_normalized_name",
                table: "perfis",
                columns: new[] { "tenant_id", "espaco_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissoes_created_by",
                table: "permissoes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_permissoes_key",
                table: "permissoes",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pessoas_created_by",
                table: "pessoas",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_planejamentos_created_by",
                table: "planejamentos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_planejamentos_tenant_id_espaco_id_person_id",
                table: "planejamentos",
                columns: new[] { "tenant_id", "espaco_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "IX_planejamentos_tenant_id_espaco_id_title_id_person_id",
                table: "planejamentos",
                columns: new[] { "tenant_id", "espaco_id", "title_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_posicoes_conta_created_by",
                table: "posicoes_conta",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_posicoes_conta_tenant_id_espaco_id_account_id_position_date",
                table: "posicoes_conta",
                columns: new[] { "tenant_id", "espaco_id", "account_id", "position_date" });

            migrationBuilder.CreateIndex(
                name: "IX_preferencias_espaco_created_by",
                table: "preferencias_espaco",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_preferencias_espaco_tenant_id_espaco_id",
                table: "preferencias_espaco",
                columns: new[] { "tenant_id", "espaco_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recorrencia_vigencias_created_by",
                table: "recorrencia_vigencias",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_recorrencia_vigencias_tenant_id_espaco_id_recurrence_id_sta~",
                table: "recorrencia_vigencias",
                columns: new[] { "tenant_id", "espaco_id", "recurrence_id", "start_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recorrencias_created_by",
                table: "recorrencias",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_recorrencias_tenant_id_espaco_id_obligation_id",
                table: "recorrencias",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referencias_informadas_created_by",
                table: "referencias_informadas",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_referencias_informadas_tenant_id_espaco_id_obligation_id",
                table: "referencias_informadas",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_referencias_informadas_tenant_id_espaco_id_title_id",
                table: "referencias_informadas",
                columns: new[] { "tenant_id", "espaco_id", "title_id" });

            migrationBuilder.CreateIndex(
                name: "IX_referencias_informadas_tenant_id_espaco_id_travel_id",
                table: "referencias_informadas",
                columns: new[] { "tenant_id", "espaco_id", "travel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_usuario_active_tenant_id_active_space_id",
                table: "sessoes_usuario",
                columns: new[] { "active_tenant_id", "active_space_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_usuario_user_id",
                table: "sessoes_usuario",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_created_by",
                table: "tenants",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_public_identifier",
                table: "tenants",
                column: "public_identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_titulos_created_by",
                table: "titulos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_titulos_tenant_id_espaco_id_card_id",
                table: "titulos",
                columns: new[] { "tenant_id", "espaco_id", "card_id" });

            migrationBuilder.CreateIndex(
                name: "IX_titulos_tenant_id_espaco_id_due_date",
                table: "titulos",
                columns: new[] { "tenant_id", "espaco_id", "due_date" },
                filter: "\"state\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_titulos_tenant_id_espaco_id_obligation_id_installment_number",
                table: "titulos",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id", "installment_number" },
                unique: true,
                filter: "\"installment_number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_titulos_tenant_id_espaco_id_obligation_id_occurrence_key",
                table: "titulos",
                columns: new[] { "tenant_id", "espaco_id", "obligation_id", "occurrence_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_titulos_tenant_id_espaco_id_period",
                table: "titulos",
                columns: new[] { "tenant_id", "espaco_id", "period" });

            migrationBuilder.CreateIndex(
                name: "IX_titulos_tenant_id_espaco_id_responsible_id_period",
                table: "titulos",
                columns: new[] { "tenant_id", "espaco_id", "responsible_id", "period" });

            migrationBuilder.CreateIndex(
                name: "IX_viagens_created_by",
                table: "viagens",
                column: "created_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acordo_titulos");

            migrationBuilder.DropTable(
                name: "ajustes_titulo");

            migrationBuilder.DropTable(
                name: "alocacoes_pagamento");

            migrationBuilder.DropTable(
                name: "anotacoes");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "baixas_historicas");

            migrationBuilder.DropTable(
                name: "cenario_quitacao_itens");

            migrationBuilder.DropTable(
                name: "fatura_itens");

            migrationBuilder.DropTable(
                name: "idempotencia");

            migrationBuilder.DropTable(
                name: "item_conversoes");

            migrationBuilder.DropTable(
                name: "membro_perfis");

            migrationBuilder.DropTable(
                name: "onboarding_operacoes");

            migrationBuilder.DropTable(
                name: "perfil_permissoes");

            migrationBuilder.DropTable(
                name: "planejamentos");

            migrationBuilder.DropTable(
                name: "posicoes_conta");

            migrationBuilder.DropTable(
                name: "preferencias_espaco");

            migrationBuilder.DropTable(
                name: "recorrencia_vigencias");

            migrationBuilder.DropTable(
                name: "referencias_informadas");

            migrationBuilder.DropTable(
                name: "sessoes_usuario");

            migrationBuilder.DropTable(
                name: "acordos_quitacao");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "cenarios_quitacao");

            migrationBuilder.DropTable(
                name: "faturas");

            migrationBuilder.DropTable(
                name: "cotacoes");

            migrationBuilder.DropTable(
                name: "itens_viagem");

            migrationBuilder.DropTable(
                name: "membros_espaco");

            migrationBuilder.DropTable(
                name: "perfis");

            migrationBuilder.DropTable(
                name: "permissoes");

            migrationBuilder.DropTable(
                name: "recorrencias");

            migrationBuilder.DropTable(
                name: "pagamentos");

            migrationBuilder.DropTable(
                name: "titulos");

            migrationBuilder.DropTable(
                name: "viagens");

            migrationBuilder.DropTable(
                name: "membros_tenant");

            migrationBuilder.DropTable(
                name: "contas");

            migrationBuilder.DropTable(
                name: "cartoes");

            migrationBuilder.DropTable(
                name: "obrigacoes");

            migrationBuilder.DropTable(
                name: "bandeiras");

            migrationBuilder.DropTable(
                name: "instituicoes");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "pessoas");

            migrationBuilder.DropTable(
                name: "espacos");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
