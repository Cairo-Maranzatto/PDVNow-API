using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PDVNow.Migrations
{
    /// <inheritdoc />
    public partial class AddCashRegisterModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_override_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Justification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_override_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "admin_override_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Justification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedAdminOverrideCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_override_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_registers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_registers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_session_reopen_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReopenedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReopenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Justification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AdminOverrideCodeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_session_reopen_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpeningFloatAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ClosingCountedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ClosingNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_sessions_cash_registers_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "cash_registers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AdminOverrideCodeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_movements_admin_override_codes_AdminOverrideCodeId",
                        column: x => x.AdminOverrideCodeId,
                        principalTable: "admin_override_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cash_movements_cash_sessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "cash_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cash_session_denominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Denomination = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_session_denominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_session_denominations_cash_sessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "cash_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_override_codes_CodeHash",
                table: "admin_override_codes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_override_codes_ExpiresAtUtc",
                table: "admin_override_codes",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_admin_override_codes_UsedAtUtc",
                table: "admin_override_codes",
                column: "UsedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_admin_override_requests_ApprovedAtUtc",
                table: "admin_override_requests",
                column: "ApprovedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_admin_override_requests_RequestedAtUtc",
                table: "admin_override_requests",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_AdminOverrideCodeId",
                table: "cash_movements",
                column: "AdminOverrideCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_CashSessionId",
                table: "cash_movements",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_CreatedAtUtc",
                table: "cash_movements",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_Code",
                table: "cash_registers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_Name",
                table: "cash_registers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_session_denominations_CashSessionId",
                table: "cash_session_denominations",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_session_denominations_CashSessionId_Denomination",
                table: "cash_session_denominations",
                columns: new[] { "CashSessionId", "Denomination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_session_reopen_events_CashSessionId",
                table: "cash_session_reopen_events",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_session_reopen_events_ReopenedAtUtc",
                table: "cash_session_reopen_events",
                column: "ReopenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_CashRegisterId",
                table: "cash_sessions",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_CashRegisterId_ClosedAtUtc",
                table: "cash_sessions",
                columns: new[] { "CashRegisterId", "ClosedAtUtc" },
                unique: true,
                filter: "\"ClosedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cash_sessions_OpenedAtUtc",
                table: "cash_sessions",
                column: "OpenedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_override_requests");

            migrationBuilder.DropTable(
                name: "cash_movements");

            migrationBuilder.DropTable(
                name: "cash_session_denominations");

            migrationBuilder.DropTable(
                name: "cash_session_reopen_events");

            migrationBuilder.DropTable(
                name: "admin_override_codes");

            migrationBuilder.DropTable(
                name: "cash_sessions");

            migrationBuilder.DropTable(
                name: "cash_registers");
        }
    }
}
