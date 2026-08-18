-- Seed sample data for development/testing
-- WARNING: Review before running. Intended for local/dev only.
-- Run with: sqlcmd -S (localdb)\MSSQLLocalDB -i DataAccess\Seeds\seed_sample_data.sql

SET NOCOUNT ON;

BEGIN TRANSACTION;

-- Companies
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'Seed Company A')
BEGIN
	INSERT INTO Companies (Name, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('Seed Company A', GETUTCDATE(), 1, 0, 'seed', 'seed');
END

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'Seed Company B')
BEGIN
	INSERT INTO Companies (Name, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('Seed Company B', GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- Customers
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Name = 'John Doe' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A'))
BEGIN
	INSERT INTO Customers (Name, Phone, Email, Address, Identification, Notes, IsGeneric, CurrentDebt, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('John Doe', '555-0100', 'john@example.com', '123 Main St', 'ID123', 'Seed customer', 0, 0.0, (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

IF NOT EXISTS (SELECT 1 FROM Customers WHERE Name = 'Jane Smith' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company B'))
BEGIN
	INSERT INTO Customers (Name, Phone, Email, Address, Identification, Notes, IsGeneric, CurrentDebt, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('Jane Smith', '555-0200', 'jane@example.com', '456 Market St', 'ID456', 'Seed customer B', 0, 0.0, (SELECT Id FROM Companies WHERE Name = 'Seed Company B'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- Resources (for appointments)
IF NOT EXISTS (SELECT 1 FROM Resources WHERE Name = 'Room A' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A'))
BEGIN
	INSERT INTO Resources (Name, Type, IsActive, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('Room A', 'Room', 1, (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

IF NOT EXISTS (SELECT 1 FROM Resources WHERE Name = 'Room B' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company B'))
BEGIN
	INSERT INTO Resources (Name, Type, IsActive, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('Room B', 'Room', 1, (SELECT Id FROM Companies WHERE Name = 'Seed Company B'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- Appointments (one overlapping scenario for Company A)
-- Create a base appointment
IF NOT EXISTS (SELECT 1 FROM Appointments WHERE CustomerId IS NOT NULL AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A') AND StartAt = '2026-08-10T10:00:00')
BEGIN
	INSERT INTO Appointments (StartAt, EndAt, Status, CustomerId, ServiceId, ResourceId, Notes, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('2026-08-10T10:00:00', '2026-08-10T11:00:00', 0, (SELECT Id FROM Customers WHERE Name = 'John Doe' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), NULL, (SELECT Id FROM Resources WHERE Name = 'Room A' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), 'Seed appointment 1', (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- Add an overlapping appointment (for test, will be rejected by API due to overlap)
IF NOT EXISTS (SELECT 1 FROM Appointments WHERE StartAt = '2026-08-10T10:30:00' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A'))
BEGIN
	INSERT INTO Appointments (StartAt, EndAt, Status, CustomerId, ServiceId, ResourceId, Notes, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ('2026-08-10T10:30:00', '2026-08-10T11:30:00', 0, (SELECT Id FROM Customers WHERE Name = 'John Doe' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), NULL, (SELECT Id FROM Resources WHERE Name = 'Room A' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), 'Seed appointment overlap', (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- Availabilities
IF NOT EXISTS (SELECT 1 FROM Availabilities WHERE ResourceId = (SELECT Id FROM Resources WHERE Name = 'Room A') AND StartAt = '2026-08-11T09:00:00')
BEGIN
	INSERT INTO Availabilities (ResourceId, StartAt, EndAt, IsBlocked, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ((SELECT Id FROM Resources WHERE Name = 'Room A' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), '2026-08-11T09:00:00', '2026-08-11T10:00:00', 1, (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- Credits for two companies
IF NOT EXISTS (SELECT 1 FROM Credits WHERE CustomerId = (SELECT Id FROM Customers WHERE Name = 'John Doe') AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A'))
BEGIN
	INSERT INTO Credits (CustomerId, TotalAmount, PaidAmount, Balance, DueDate, Status, MinimumPaymentAmount, PaymentFrequency, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ((SELECT Id FROM Customers WHERE Name = 'John Doe' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), 1000.00, 100.00, 900.00, '2026-07-01', 0, 50.00, 'monthly', (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

IF NOT EXISTS (SELECT 1 FROM Credits WHERE CustomerId = (SELECT Id FROM Customers WHERE Name = 'Jane Smith') AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company B'))
BEGIN
	INSERT INTO Credits (CustomerId, TotalAmount, PaidAmount, Balance, DueDate, Status, MinimumPaymentAmount, PaymentFrequency, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ((SELECT Id FROM Customers WHERE Name = 'Jane Smith' AND CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company B')), 500.00, 0.00, 500.00, '2026-07-01', 0, 25.00, 'monthly', (SELECT Id FROM Companies WHERE Name = 'Seed Company B'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- CreditPayments for first credit (partial)
IF NOT EXISTS (SELECT 1 FROM CreditPayments WHERE CreditId = (SELECT TOP 1 Id FROM Credits WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')) AND Amount = 100.00)
BEGIN
	INSERT INTO CreditPayments (CreditId, Amount, PaidAt, Notes, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ((SELECT TOP 1 Id FROM Credits WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), 100.00, GETUTCDATE(), 'Seed initial payment', (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

-- CreditStatusHistory for initial state (optional)
IF NOT EXISTS (SELECT 1 FROM CreditStatusHistory WHERE CreditId = (SELECT TOP 1 Id FROM Credits WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')) AND NewStatus = 'PENDING')
BEGIN
	INSERT INTO CreditStatusHistory (CreditId, OldStatus, NewStatus, ChangedAt, ChangedByUserId, CompanyId, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES ((SELECT TOP 1 Id FROM Credits WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = 'Seed Company A')), '', 'PENDING', GETUTCDATE(), 0, (SELECT Id FROM Companies WHERE Name = 'Seed Company A'), GETUTCDATE(), 1, 0, 'seed', 'seed');
END

COMMIT TRANSACTION;

PRINT 'Seed script executed (no errors returned). Review inserted rows.';
