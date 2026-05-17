using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Audit.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    user_full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    scope_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    persona_side = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    roles_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_system_session = table.Column<bool>(type: "boolean", nullable: false),
                    support_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    impersonated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    browser_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    browser_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    device_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    accept_language = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    referer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    country = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    city = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    request_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    request_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    environment_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    machine_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    application_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    application_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    process_id = table.Column<int>(type: "integer", nullable: false),
                    thread_id = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<short>(type: "smallint", nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character(9)", fixedLength: true, maxLength: 9, nullable: false),
                    scope = table.Column<short>(type: "smallint", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_built_in = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "citext", nullable: true),
                    normalized_name = table.Column<string>(type: "citext", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character(9)", fixedLength: true, maxLength: 9, nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 256, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    billing_tier = table.Column<short>(type: "smallint", nullable: false),
                    has_dedicated_database = table.Column<bool>(type: "boolean", nullable: false),
                    database_schema_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "url_code_registry",
                columns: table => new
                {
                    code = table.Column<string>(type: "character(9)", fixedLength: true, maxLength: 9, nullable: false),
                    owner_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_url_code_registry", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character(9)", fixedLength: true, maxLength: 9, nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tckn = table.Column<string>(type: "character(11)", fixedLength: true, maxLength: 11, nullable: true),
                    tckn_verified = table.Column<bool>(type: "boolean", nullable: false),
                    vkn = table.Column<string>(type: "character(10)", fixedLength: true, maxLength: 10, nullable: true),
                    vkn_verified = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_name = table.Column<string>(type: "citext", nullable: true),
                    normalized_user_name = table.Column<string>(type: "citext", nullable: true),
                    email = table.Column<string>(type: "citext", nullable: true),
                    normalized_email = table.Column<string>(type: "citext", nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                    table.CheckConstraint("ck_user_tckn_format", "tckn IS NULL OR tckn ~ '^[0-9]{11}$'");
                    table.CheckConstraint("ck_user_vkn_format", "vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'");
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permission_permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_connection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_string_encrypted = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_connection", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_connection_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "support_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character(9)", fixedLength: true, maxLength: 9, nullable: false),
                    operator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    reason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    write_action_count = table.Column<int>(type: "integer", nullable: false),
                    customer_notified = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_session", x => x.id);
                    table.CheckConstraint("ck_support_session_reason_minlength", "char_length(reason) >= 20");
                    table.ForeignKey(
                        name: "fk_support_session_tenant_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_session_user_operator_user_id",
                        column: x => x.operator_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_role_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_level = table.Column<short>(type: "smallint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role_assignment", x => x.id);
                    table.CheckConstraint("ck_user_role_assignment_scope_consistency", "(scope_level = 1      AND tenant_id IS NULL     AND company_id IS NULL AND unit_id IS NULL)\nOR (scope_level = 2   AND tenant_id IS NOT NULL AND company_id IS NULL AND unit_id IS NULL)\nOR (scope_level = 3  AND tenant_id IS NOT NULL AND company_id IS NOT NULL AND unit_id IS NULL)\nOR (scope_level = 4     AND tenant_id IS NOT NULL AND company_id IS NOT NULL AND unit_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_user_role_assignment_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_role_assignment_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_entity",
                table: "audit_entries",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_support_session",
                table: "audit_entries",
                column: "support_session_id",
                filter: "support_session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_tenant_timestamp",
                table: "audit_entries",
                columns: new[] { "tenant_id", "timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_permission_code",
                table: "permission",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permission_module",
                table: "permission",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token_hash",
                table: "refresh_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id_context_id",
                table: "refresh_token",
                columns: new[] { "user_id", "context_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_normalized_name_scope",
                table: "role",
                columns: new[] { "normalized_name", "scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_url_code",
                table: "role",
                column: "url_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_session_operator_started",
                table: "support_session",
                columns: new[] { "operator_user_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_support_session_tenant_started",
                table: "support_session",
                columns: new[] { "target_tenant_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_support_session_url_code",
                table: "support_session",
                column: "url_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_name",
                table: "tenant",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_url_code",
                table: "tenant",
                column: "url_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_connection_tenant_id",
                table: "tenant_connection",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_connection_tenant_id_is_active",
                table: "tenant_connection",
                columns: new[] { "tenant_id", "is_active" },
                unique: true,
                filter: "is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_url_code_registry_owner_type_owner_id",
                table: "url_code_registry",
                columns: new[] { "owner_type", "owner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_tckn",
                table: "user",
                column: "tckn",
                unique: true,
                filter: "tckn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_url_code",
                table: "user",
                column: "url_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_vkn",
                table: "user",
                column: "vkn",
                unique: true,
                filter: "vkn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignment_role_id",
                table: "user_role_assignment",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignment_unique",
                table: "user_role_assignment",
                columns: new[] { "user_id", "role_id", "scope_level", "tenant_id", "company_id", "unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignment_user_id_is_active",
                table: "user_role_assignment",
                columns: new[] { "user_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "support_session");

            migrationBuilder.DropTable(
                name: "tenant_connection");

            migrationBuilder.DropTable(
                name: "url_code_registry");

            migrationBuilder.DropTable(
                name: "user_role_assignment");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "tenant");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
