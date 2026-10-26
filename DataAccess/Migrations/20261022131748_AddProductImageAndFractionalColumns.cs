using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageAndFractionalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'AllowFractional')
    ALTER TABLE [Products] ADD [AllowFractional] bit NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'ImageUrl')
    ALTER TABLE [Products] ADD [ImageUrl] nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'ProfitMargin')
    ALTER TABLE [Products] ADD [ProfitMargin] decimal(18,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Sales]') AND name = N'CancellationReason')
    ALTER TABLE [Sales] ADD [CancellationReason] nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Sales]') AND name = N'CancelledAt')
    ALTER TABLE [Sales] ADD [CancelledAt] datetime2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CompanySettings]') AND name = N'InvoiceFooterPhrase')
    ALTER TABLE [CompanySettings] ADD [InvoiceFooterPhrase] nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CompanySettings]') AND name = N'TicketInstagram')
    ALTER TABLE [CompanySettings] ADD [TicketInstagram] nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CompanySettings]') AND name = N'TicketPaperSize')
    ALTER TABLE [CompanySettings] ADD [TicketPaperSize] nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CompanySettings]') AND name = N'TicketShowQr')
    ALTER TABLE [CompanySettings] ADD [TicketShowQr] bit NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[CompanySettings]') AND name = N'TicketWhatsapp')
    ALTER TABLE [CompanySettings] ADD [TicketWhatsapp] nvarchar(max) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'AllowFractional')
    ALTER TABLE [Products] DROP COLUMN [AllowFractional];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'ImageUrl')
    ALTER TABLE [Products] DROP COLUMN [ImageUrl];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'ProfitMargin')
    ALTER TABLE [Products] DROP COLUMN [ProfitMargin];
");
        }
    }
}
