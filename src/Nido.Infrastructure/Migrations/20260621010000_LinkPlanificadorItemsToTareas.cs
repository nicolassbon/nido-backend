using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [Migration("20260621010000_LinkPlanificadorItemsToTareas")]
    public partial class LinkPlanificadorItemsToTareas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tarea_id",
                table: "planificador_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_planificador_item_tarea",
                table: "planificador_item",
                column: "tarea_id");

            migrationBuilder.AddForeignKey(
                name: "planificador_item_tarea_id_fkey",
                table: "planificador_item",
                column: "tarea_id",
                principalTable: "tareas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "planificador_item_tarea_id_fkey",
                table: "planificador_item");

            migrationBuilder.DropIndex(
                name: "idx_planificador_item_tarea",
                table: "planificador_item");

            migrationBuilder.DropColumn(
                name: "tarea_id",
                table: "planificador_item");
        }
    }
}
