using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMgmt.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingTemplateAndGenerationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `SchedulingTemplates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TemplateName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `WeekType` int NOT NULL,
    `WeekIndex` int NOT NULL,
    `DayOfWeek` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `ShiftType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_SchedulingTemplates` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `TemplateGenerationLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TemplateName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `WeekType` int NOT NULL,
    `StartDate` date NOT NULL,
    `EndDate` date NOT NULL,
    `GeneratedAt` datetime(6) NOT NULL,
    `GeneratedCount` int NOT NULL,
    CONSTRAINT `PK_TemplateGenerationLogs` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;");

            migrationBuilder.Sql(@"
CREATE INDEX `IX_TemplateGenerationLogs_TemplateName_StartDate_EndDate`
ON `TemplateGenerationLogs` (`TemplateName`, `StartDate`, `EndDate`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `TemplateGenerationLogs`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `SchedulingTemplates`;");
        }
    }
}
