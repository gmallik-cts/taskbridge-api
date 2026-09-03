using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskBridge.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStateSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    NewStateSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorOrganizationId_ProjectId_Timestamp_EventT~",
                table: "AuditEntries",
                columns: new[] { "ActorOrganizationId", "ProjectId", "Timestamp", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_SourceEventId",
                table: "AuditEntries",
                column: "SourceEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AuditEntryId_RecipientUserId",
                table: "Notifications",
                columns: new[] { "AuditEntryId", "RecipientUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId_ProjectId",
                table: "Notifications",
                columns: new[] { "OrganizationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId_RecipientUserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "OrganizationId", "RecipientUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
