using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace DatabaseCreator.Migrations
{
    public partial class FirstMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_ftp_info",
                columns: table => new
                {
                    id_client_ftp_info = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_company_id = table.Column<int>(type: "integer", nullable: false),
                    ftp_directory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_ftp_info", x => x.id_client_ftp_info);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id_order = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    client_company_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_file = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    response_file = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    creation_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    modification_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ftp_status = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    wholesaler = table.Column<int>(type: "integer", nullable: false),
                    ftp_file = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id_order);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id_product = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    central_ident_number = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    processed_quantity = table.Column<int>(type: "integer", nullable: false),
                    order_fk = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id_product);
                    table.ForeignKey(
                        name: "FK_products_orders_order_fk",
                        column: x => x.order_fk,
                        principalTable: "orders",
                        principalColumn: "id_order",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_order_fk",
                table: "products",
                column: "order_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_ftp_info");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
