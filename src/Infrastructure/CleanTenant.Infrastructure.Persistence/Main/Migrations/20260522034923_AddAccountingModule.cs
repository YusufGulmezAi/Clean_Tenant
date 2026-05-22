using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 12, nullable: false),
                    parent_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    name = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    account_class = table.Column<short>(type: "smallint", nullable: false),
                    account_type = table.Column<short>(type: "smallint", nullable: false),
                    source = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_detail = table.Column<bool>(type: "boolean", nullable: false),
                    is_monetary = table.Column<bool>(type: "boolean", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    template_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_codes", x => x.id);
                    table.CheckConstraint("ck_account_code_format", "code ~ '^[1-9][0-9]{2}(\\.[0-9]{2}(\\.[0-9]{3})?)?$'");
                    table.CheckConstraint("ck_account_code_level_match", "(level = 0 AND char_length(replace(code, '.', '')) = 3) OR (level = 1 AND char_length(replace(code, '.', '')) = 6) OR (level = 2 AND char_length(replace(code, '.', '')) = 9)");
                });

            migrationBuilder.CreateTable(
                name: "accounting_bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    iban = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    account_type = table.Column<short>(type: "smallint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    account_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_bank_accounts", x => x.id);
                    table.CheckConstraint("ck_bank_iban_format", "iban IS NULL OR iban ~ '^TR[0-9]{24}$'");
                });

            migrationBuilder.CreateTable(
                name: "accounting_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_activated = table.Column<bool>(type: "boolean", nullable: false),
                    require_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    default_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    vat_period = table.Column<short>(type: "smallint", nullable: false),
                    e_defter_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budgeted_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budgets", x => x.id);
                    table.CheckConstraint("ck_budget_amount", "budgeted_amount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "cost_centers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_centers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entry_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<short>(type: "smallint", nullable: false),
                    last_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entry_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_years",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    is_current_year = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscal_years", x => x.id);
                    table.CheckConstraint("ck_fiscal_year_dates", "end_date > start_date");
                    table.CheckConstraint("ck_fiscal_year_duration", "end_date - start_date BETWEEN 330 AND 400");
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    direction = table.Column<short>(type: "smallint", nullable: false),
                    counterparty_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    counterparty_tax_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    vat_category = table.Column<short>(type: "smallint", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_posted_to_journal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.CheckConstraint("ck_invoice_amounts", "sub_total >= 0 AND vat_amount >= 0 AND total_amount = sub_total + vat_amount");
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<short>(type: "smallint", nullable: false),
                    entry_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_debit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_credit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    posted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    e_defter_xml = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_entries", x => x.id);
                    table.CheckConstraint("ck_journal_balanced", "status != 2 OR total_debit = total_credit");
                });

            migrationBuilder.CreateTable(
                name: "accounting_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_periods", x => x.id);
                    table.CheckConstraint("ck_accounting_period_dates", "end_date > start_date");
                    table.CheckConstraint("ck_accounting_period_month", "month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "fk_accounting_periods_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code_value = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    original_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_lines", x => x.id);
                    table.CheckConstraint("ck_journal_line_sign", "debit >= 0 AND credit >= 0 AND (debit = 0 OR credit = 0)");
                    table.ForeignKey(
                        name: "fk_journal_lines_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_codes_company_code",
                table: "account_codes",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_account_codes_company_id",
                table: "account_codes",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_codes_tenant_id",
                table: "account_codes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_bank_accounts_account_code_id",
                table: "accounting_bank_accounts",
                column: "account_code_id",
                filter: "account_code_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_bank_accounts_company_id",
                table: "accounting_bank_accounts",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_bank_accounts_tenant_id",
                table: "accounting_bank_accounts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_company_id",
                table: "accounting_periods",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_company_year_month",
                table: "accounting_periods",
                columns: new[] { "company_id", "year", "month" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_fiscal_year_id",
                table: "accounting_periods",
                column: "fiscal_year_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_tenant_id",
                table: "accounting_periods",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_settings_company",
                table: "accounting_settings",
                column: "company_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_settings_tenant_id",
                table: "accounting_settings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_budgets_account_code_id",
                table: "budgets",
                column: "account_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_budgets_accounting_period_id",
                table: "budgets",
                column: "accounting_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_budgets_company_id",
                table: "budgets",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_budgets_company_period_account_costcenter",
                table: "budgets",
                columns: new[] { "company_id", "accounting_period_id", "account_code_id", "cost_center_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budgets_tenant_id",
                table: "budgets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_company_code",
                table: "cost_centers",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_company_id",
                table: "cost_centers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_tenant_id",
                table: "cost_centers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_entry_sequences_company_id",
                table: "entry_sequences",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_entry_sequences_company_year_type",
                table: "entry_sequences",
                columns: new[] { "company_id", "fiscal_year_id", "entry_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entry_sequences_fiscal_year_id",
                table: "entry_sequences",
                column: "fiscal_year_id");

            migrationBuilder.CreateIndex(
                name: "ix_entry_sequences_tenant_id",
                table: "entry_sequences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_years_company_id",
                table: "fiscal_years",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_years_company_start",
                table: "fiscal_years",
                columns: new[] { "company_id", "start_date" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_years_tenant_id",
                table: "fiscal_years",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_account_code_id",
                table: "invoices",
                column: "account_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_accounting_period_id",
                table: "invoices",
                column: "accounting_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id",
                table: "invoices",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_number_direction",
                table: "invoices",
                columns: new[] { "company_id", "invoice_number", "direction" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_journal_entry_id",
                table: "invoices",
                column: "journal_entry_id",
                filter: "journal_entry_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id",
                table: "invoices",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_accounting_period_id",
                table: "journal_entries",
                column: "accounting_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_company_id",
                table: "journal_entries",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_company_number",
                table: "journal_entries",
                columns: new[] { "company_id", "entry_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_tenant_id",
                table: "journal_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_account_code_id",
                table: "journal_lines",
                column: "account_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_company_id",
                table: "journal_lines",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_cost_center_id",
                table: "journal_lines",
                column: "cost_center_id",
                filter: "cost_center_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_journal_entry_id",
                table: "journal_lines",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_tenant_id",
                table: "journal_lines",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_codes");

            migrationBuilder.DropTable(
                name: "accounting_bank_accounts");

            migrationBuilder.DropTable(
                name: "accounting_periods");

            migrationBuilder.DropTable(
                name: "accounting_settings");

            migrationBuilder.DropTable(
                name: "budgets");

            migrationBuilder.DropTable(
                name: "cost_centers");

            migrationBuilder.DropTable(
                name: "entry_sequences");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "journal_lines");

            migrationBuilder.DropTable(
                name: "fiscal_years");

            migrationBuilder.DropTable(
                name: "journal_entries");
        }
    }
}
