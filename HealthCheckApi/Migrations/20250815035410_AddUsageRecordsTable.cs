using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCheckApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsageRecord_Items_ItemId",
                table: "UsageRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsageRecord",
                table: "UsageRecord");

            migrationBuilder.RenameTable(
                name: "UsageRecord",
                newName: "UsageRecords");

            migrationBuilder.RenameIndex(
                name: "IX_UsageRecord_ItemId",
                table: "UsageRecords",
                newName: "IX_UsageRecords_ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsageRecords",
                table: "UsageRecords",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UsageRecords_Items_ItemId",
                table: "UsageRecords",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsageRecords_Items_ItemId",
                table: "UsageRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsageRecords",
                table: "UsageRecords");

            migrationBuilder.RenameTable(
                name: "UsageRecords",
                newName: "UsageRecord");

            migrationBuilder.RenameIndex(
                name: "IX_UsageRecords_ItemId",
                table: "UsageRecord",
                newName: "IX_UsageRecord_ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsageRecord",
                table: "UsageRecord",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UsageRecord_Items_ItemId",
                table: "UsageRecord",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
