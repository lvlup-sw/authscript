using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registered_patients",
                columns: table => new
                {
                    PatientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EncounterId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PracticeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WorkItemId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentEncounterStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registered_patients", x => x.PatientId);
                });

            migrationBuilder.CreateTable(
                name: "work_items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EncounterId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceRequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProcedureCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_registered_patients_registered",
                table: "registered_patients",
                column: "RegisteredAt");

            migrationBuilder.CreateIndex(
                name: "idx_work_items_encounter",
                table: "work_items",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "idx_work_items_status",
                table: "work_items",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registered_patients");

            migrationBuilder.DropTable(
                name: "work_items");
        }
    }
}
