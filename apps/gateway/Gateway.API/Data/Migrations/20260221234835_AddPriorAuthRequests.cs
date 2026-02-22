using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorAuthRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prior_auth_requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FhirPatientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientMrn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientDob = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PatientMemberId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PatientPayer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PatientAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PatientPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ProcedureCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProcedureName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DiagnosisCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DiagnosisName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProviderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProviderNpi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ServiceDate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PlaceOfService = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClinicalSummary = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    CriteriaJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewTimeSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prior_auth_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_prior_auth_requests_status",
                table: "prior_auth_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_prior_auth_requests_created_at",
                table: "prior_auth_requests",
                column: "CreatedAt",
                descending: [true]);

            migrationBuilder.CreateIndex(
                name: "idx_prior_auth_requests_patient",
                table: "prior_auth_requests",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prior_auth_requests");
        }
    }
}
