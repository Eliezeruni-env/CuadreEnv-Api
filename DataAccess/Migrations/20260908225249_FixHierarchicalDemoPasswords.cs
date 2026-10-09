using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixHierarchicalDemoPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Users
SET PasswordHash = N'$2a$11$fwsUE3NQ7K5Rl69qFuRedOtzKiFEGOb8gUXQbxU20Dg4TfVcQhR0W',
    ModificationDate = GETUTCDATE(),
    ModifiedBy = N'seed-password-fix'
WHERE Email IN (
    N'hier.admin.a@cuadreenv.local',
    N'hier.admin.b@cuadreenv.local',
    N'hier.audit.a@cuadreenv.local',
    N'hier.audit.b@cuadreenv.local',
    N'hier.sales.a@cuadreenv.local',
    N'hier.sales.b@cuadreenv.local',
    N'hier.inventory.a@cuadreenv.local',
    N'hier.inventory.b@cuadreenv.local',
    N'hier.employee.a@cuadreenv.local',
    N'hier.employee.b@cuadreenv.local'
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Users
SET PasswordHash = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq',
    ModificationDate = GETUTCDATE(),
    ModifiedBy = N'seed-password-rollback'
WHERE Email LIKE N'hier.%@cuadreenv.local';
");
        }
    }
}
