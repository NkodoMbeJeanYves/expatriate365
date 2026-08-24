using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributionCharges_ContributionTypes_ContributionTypeId",
                table: "ContributionCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributionCharges_Members_MemberId",
                table: "ContributionCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributionTypes_Tenants_TenantId",
                table: "ContributionTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ContributionCharges_ChargeId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Members_MemberId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributionTypes",
                table: "ContributionTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributionCharges",
                table: "ContributionCharges");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "payments");

            migrationBuilder.RenameTable(
                name: "ContributionTypes",
                newName: "contribution_types");

            migrationBuilder.RenameTable(
                name: "ContributionCharges",
                newName: "contribution_charges");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "payments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "payments",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "payments",
                newName: "currency");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "payments",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "payments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "payments",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "payments",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "ReversedBy",
                table: "payments",
                newName: "reversed_by");

            migrationBuilder.RenameColumn(
                name: "ReversedAt",
                table: "payments",
                newName: "reversed_at");

            migrationBuilder.RenameColumn(
                name: "ReversalReason",
                table: "payments",
                newName: "reversal_reason");

            migrationBuilder.RenameColumn(
                name: "ReceiptNumber",
                table: "payments",
                newName: "receipt_number");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodId",
                table: "payments",
                newName: "payment_method_id");

            migrationBuilder.RenameColumn(
                name: "PaymentGateway",
                table: "payments",
                newName: "payment_gateway");

            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "payments",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "payments",
                newName: "member_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "payments",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "GatewayTransactionId",
                table: "payments",
                newName: "gateway_transaction_id");

            migrationBuilder.RenameColumn(
                name: "GatewayReference",
                table: "payments",
                newName: "gateway_reference");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "payments",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConfirmedBy",
                table: "payments",
                newName: "confirmed_by");

            migrationBuilder.RenameColumn(
                name: "ConfirmedAt",
                table: "payments",
                newName: "confirmed_at");

            migrationBuilder.RenameColumn(
                name: "ChargeId",
                table: "payments",
                newName: "charge_id");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_MemberId",
                table: "payments",
                newName: "IX_payments_member_id");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ChargeId",
                table: "payments",
                newName: "IX_payments_charge_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "contribution_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Frequency",
                table: "contribution_types",
                newName: "frequency");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "contribution_types",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "contribution_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "contribution_types",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "contribution_types",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "LatePenaltyRate",
                table: "contribution_types",
                newName: "late_penalty_rate");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "contribution_types",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "GracePeriodDays",
                table: "contribution_types",
                newName: "grace_period_days");

            migrationBuilder.RenameColumn(
                name: "EffectiveTo",
                table: "contribution_types",
                newName: "effective_to");

            migrationBuilder.RenameColumn(
                name: "EffectiveFrom",
                table: "contribution_types",
                newName: "effective_from");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "contribution_types",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BaseAmount",
                table: "contribution_types",
                newName: "base_amount");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionTypes_TenantId",
                table: "contribution_types",
                newName: "IX_contribution_types_tenant_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "contribution_charges",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "contribution_charges",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WaiverAmount",
                table: "contribution_charges",
                newName: "waiver_amount");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "contribution_charges",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "contribution_charges",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "PenaltyAmount",
                table: "contribution_charges",
                newName: "penalty_amount");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "contribution_charges",
                newName: "member_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "contribution_charges",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "contribution_charges",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "contribution_charges",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContributionTypeId",
                table: "contribution_charges",
                newName: "contribution_type_id");

            migrationBuilder.RenameColumn(
                name: "BaseAmount",
                table: "contribution_charges",
                newName: "base_amount");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "contribution_charges",
                newName: "amount_paid");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionCharges_MemberId",
                table: "contribution_charges",
                newName: "IX_contribution_charges_member_id");

            migrationBuilder.RenameIndex(
                name: "IX_ContributionCharges_ContributionTypeId",
                table: "contribution_charges",
                newName: "IX_contribution_charges_contribution_type_id");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "payments",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "reversal_reason",
                table: "payments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "payment_gateway",
                table: "payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId1",
                table: "payments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "contribution_types",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "frequency",
                table: "contribution_types",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "contribution_types",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "late_penalty_rate",
                table: "contribution_types",
                type: "decimal(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_amount",
                table: "contribution_types",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "contribution_charges",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "waiver_amount",
                table: "contribution_charges",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "penalty_amount",
                table: "contribution_charges",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_amount",
                table: "contribution_charges",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_paid",
                table: "contribution_charges",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId1",
                table: "contribution_charges",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_contribution_types",
                table: "contribution_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_contribution_charges",
                table: "contribution_charges",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_MemberId1",
                table: "payments",
                column: "MemberId1");

            migrationBuilder.CreateIndex(
                name: "IX_contribution_charges_MemberId1",
                table: "contribution_charges",
                column: "MemberId1");

            migrationBuilder.AddForeignKey(
                name: "FK_contribution_charges_Members_MemberId1",
                table: "contribution_charges",
                column: "MemberId1",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_contribution_charges_Members_member_id",
                table: "contribution_charges",
                column: "member_id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_contribution_charges_contribution_types_contribution_type_id",
                table: "contribution_charges",
                column: "contribution_type_id",
                principalTable: "contribution_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_contribution_types_Tenants_tenant_id",
                table: "contribution_types",
                column: "tenant_id",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_Members_MemberId1",
                table: "payments",
                column: "MemberId1",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_Members_member_id",
                table: "payments",
                column: "member_id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_contribution_charges_charge_id",
                table: "payments",
                column: "charge_id",
                principalTable: "contribution_charges",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contribution_charges_Members_MemberId1",
                table: "contribution_charges");

            migrationBuilder.DropForeignKey(
                name: "FK_contribution_charges_Members_member_id",
                table: "contribution_charges");

            migrationBuilder.DropForeignKey(
                name: "FK_contribution_charges_contribution_types_contribution_type_id",
                table: "contribution_charges");

            migrationBuilder.DropForeignKey(
                name: "FK_contribution_types_Tenants_tenant_id",
                table: "contribution_types");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_Members_MemberId1",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_Members_member_id",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_contribution_charges_charge_id",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_MemberId1",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_contribution_types",
                table: "contribution_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_contribution_charges",
                table: "contribution_charges");

            migrationBuilder.DropIndex(
                name: "IX_contribution_charges_MemberId1",
                table: "contribution_charges");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "contribution_charges");

            migrationBuilder.RenameTable(
                name: "payments",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "contribution_types",
                newName: "ContributionTypes");

            migrationBuilder.RenameTable(
                name: "contribution_charges",
                newName: "ContributionCharges");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Payments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Payments",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "currency",
                table: "Payments",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Payments",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Payments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Payments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "Payments",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "reversed_by",
                table: "Payments",
                newName: "ReversedBy");

            migrationBuilder.RenameColumn(
                name: "reversed_at",
                table: "Payments",
                newName: "ReversedAt");

            migrationBuilder.RenameColumn(
                name: "reversal_reason",
                table: "Payments",
                newName: "ReversalReason");

            migrationBuilder.RenameColumn(
                name: "receipt_number",
                table: "Payments",
                newName: "ReceiptNumber");

            migrationBuilder.RenameColumn(
                name: "payment_method_id",
                table: "Payments",
                newName: "PaymentMethodId");

            migrationBuilder.RenameColumn(
                name: "payment_gateway",
                table: "Payments",
                newName: "PaymentGateway");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                table: "Payments",
                newName: "PaymentDate");

            migrationBuilder.RenameColumn(
                name: "member_id",
                table: "Payments",
                newName: "MemberId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Payments",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "gateway_transaction_id",
                table: "Payments",
                newName: "GatewayTransactionId");

            migrationBuilder.RenameColumn(
                name: "gateway_reference",
                table: "Payments",
                newName: "GatewayReference");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Payments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "confirmed_by",
                table: "Payments",
                newName: "ConfirmedBy");

            migrationBuilder.RenameColumn(
                name: "confirmed_at",
                table: "Payments",
                newName: "ConfirmedAt");

            migrationBuilder.RenameColumn(
                name: "charge_id",
                table: "Payments",
                newName: "ChargeId");

            migrationBuilder.RenameIndex(
                name: "IX_payments_member_id",
                table: "Payments",
                newName: "IX_Payments_MemberId");

            migrationBuilder.RenameIndex(
                name: "IX_payments_charge_id",
                table: "Payments",
                newName: "IX_Payments_ChargeId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ContributionTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "frequency",
                table: "ContributionTypes",
                newName: "Frequency");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "ContributionTypes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ContributionTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "ContributionTypes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "ContributionTypes",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "late_penalty_rate",
                table: "ContributionTypes",
                newName: "LatePenaltyRate");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "ContributionTypes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "grace_period_days",
                table: "ContributionTypes",
                newName: "GracePeriodDays");

            migrationBuilder.RenameColumn(
                name: "effective_to",
                table: "ContributionTypes",
                newName: "EffectiveTo");

            migrationBuilder.RenameColumn(
                name: "effective_from",
                table: "ContributionTypes",
                newName: "EffectiveFrom");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ContributionTypes",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "base_amount",
                table: "ContributionTypes",
                newName: "BaseAmount");

            migrationBuilder.RenameIndex(
                name: "IX_contribution_types_tenant_id",
                table: "ContributionTypes",
                newName: "IX_ContributionTypes_TenantId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ContributionCharges",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ContributionCharges",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "waiver_amount",
                table: "ContributionCharges",
                newName: "WaiverAmount");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "ContributionCharges",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "ContributionCharges",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "penalty_amount",
                table: "ContributionCharges",
                newName: "PenaltyAmount");

            migrationBuilder.RenameColumn(
                name: "member_id",
                table: "ContributionCharges",
                newName: "MemberId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "ContributionCharges",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "ContributionCharges",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ContributionCharges",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "contribution_type_id",
                table: "ContributionCharges",
                newName: "ContributionTypeId");

            migrationBuilder.RenameColumn(
                name: "base_amount",
                table: "ContributionCharges",
                newName: "BaseAmount");

            migrationBuilder.RenameColumn(
                name: "amount_paid",
                table: "ContributionCharges",
                newName: "AmountPaid");

            migrationBuilder.RenameIndex(
                name: "IX_contribution_charges_member_id",
                table: "ContributionCharges",
                newName: "IX_ContributionCharges_MemberId");

            migrationBuilder.RenameIndex(
                name: "IX_contribution_charges_contribution_type_id",
                table: "ContributionCharges",
                newName: "IX_ContributionCharges_ContributionTypeId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Payments",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "ReversalReason",
                table: "Payments",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentGateway",
                table: "Payments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContributionTypes",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "ContributionTypes",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ContributionTypes",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "LatePenaltyRate",
                table: "ContributionTypes",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseAmount",
                table: "ContributionTypes",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ContributionCharges",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "WaiverAmount",
                table: "ContributionCharges",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PenaltyAmount",
                table: "ContributionCharges",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseAmount",
                table: "ContributionCharges",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountPaid",
                table: "ContributionCharges",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributionTypes",
                table: "ContributionTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributionCharges",
                table: "ContributionCharges",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionCharges_ContributionTypes_ContributionTypeId",
                table: "ContributionCharges",
                column: "ContributionTypeId",
                principalTable: "ContributionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionCharges_Members_MemberId",
                table: "ContributionCharges",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributionTypes_Tenants_TenantId",
                table: "ContributionTypes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ContributionCharges_ChargeId",
                table: "Payments",
                column: "ChargeId",
                principalTable: "ContributionCharges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Members_MemberId",
                table: "Payments",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
