using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSis.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_RequestId",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_StudentId",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_Charges_BatchId",
                table: "Charges");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_RequestId_CreatedAt",
                table: "SupportMessages",
                columns: new[] { "RequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_Link",
                table: "Notifications",
                columns: new[] { "Type", "Link" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_StudentId_UpdatedAt",
                table: "ChatConversations",
                columns: new[] { "StudentId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Charges_BatchId_Status",
                table: "Charges",
                columns: new[] { "BatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Charges_Status_DueDate",
                table: "Charges",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_PublishAt",
                table: "Announcements",
                column: "PublishAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_RequestId_CreatedAt",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type_Link",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_StudentId_UpdatedAt",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_Charges_BatchId_Status",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Charges_Status_DueDate",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_PublishAt",
                table: "Announcements");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_RequestId",
                table: "SupportMessages",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_StudentId",
                table: "ChatConversations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_BatchId",
                table: "Charges",
                column: "BatchId");
        }
    }
}
