using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PDVNow.Migrations
{
    /// <inheritdoc />
    public partial class AddMovimentacoesVendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sale_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ItemDiscountTotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SaleDiscountAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_cash_registers_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "cash_registers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_cash_sessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "cash_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceOriginal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceFinal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sale_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_items_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AmountReceived = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ChangeGiven = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    AuthorizationCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sale_payments_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sale_events_PerformedAtUtc",
                table: "sale_events",
                column: "PerformedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sale_events_SaleId",
                table: "sale_events",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_ProductId",
                table: "sale_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_SaleId",
                table: "sale_items",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_SaleId_ProductId",
                table: "sale_items",
                columns: new[] { "SaleId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_CreatedAtUtc",
                table: "sale_payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_SaleId",
                table: "sale_payments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_CashRegisterId",
                table: "sales",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_CashSessionId",
                table: "sales",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_Code",
                table: "sales",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_CreatedAtUtc",
                table: "sales",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sales_CustomerId",
                table: "sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_Status",
                table: "sales",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_events");

            migrationBuilder.DropTable(
                name: "sale_items");

            migrationBuilder.DropTable(
                name: "sale_payments");

            migrationBuilder.DropTable(
                name: "sales");
        }
    }
}
