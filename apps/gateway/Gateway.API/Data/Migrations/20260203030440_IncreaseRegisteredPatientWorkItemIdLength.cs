using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseRegisteredPatientWorkItemIdLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkItemId",
                table: "registered_patients",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    DO $$
    BEGIN
      IF EXISTS (
        SELECT 1
        FROM registered_patients
        WHERE length(""WorkItemId"") > 32
      ) THEN
        RAISE EXCEPTION 'Cannot downgrade: registered_patients.WorkItemId length > 32';
      END IF;
    END $$;
");

            migrationBuilder.AlterColumn<string>(
                name: "WorkItemId",
                table: "registered_patients",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);
        }
    }
}
