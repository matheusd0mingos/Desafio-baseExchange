using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderAccumulator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exposicoes",
                columns: table => new
                {
                    ativo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exposicoes", x => x.ativo);
                });

            migrationBuilder.CreateTable(
                name: "ordens_aceitas",
                columns: table => new
                {
                    ordem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    lado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    exposicao_resultante = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurred_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_aceitas", x => x.ordem_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    chave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    publicada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "exposicoes",
                columns: new[] { "ativo", "valor" },
                values: new object[,]
                {
                    { "PETR4", 0m },
                    { "VALE3", 0m },
                    { "VIIA4", 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_criada_em",
                table: "outbox",
                column: "criada_em",
                filter: "publicada_em IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exposicoes");

            migrationBuilder.DropTable(
                name: "ordens_aceitas");

            migrationBuilder.DropTable(
                name: "outbox");
        }
    }
}
