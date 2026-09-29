SET NOCOUNT ON;
SET XACT_ABORT ON;

SELECT 'rowversion' AS CheckName, t.name AS TableName, c.name AS ColumnName
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.name IN ('CashRegisters', 'Products', 'InvoiceSequences', 'CashMovements')
  AND c.name = 'RowVersion'
  AND c.is_rowguidcol = 0;

SELECT 'trigger' AS CheckName, name AS TriggerName
FROM sys.triggers
WHERE name IN ('TR_CashRegisters_ImmutableAfterClose', 'TR_CashMovements_AppendOnly');

SELECT CompanyId, COUNT(*) AS DuplicateCount
FROM dbo.InvoiceSequences
GROUP BY CompanyId
HAVING COUNT(*) > 1;

SELECT CompanyId, DocumentKey, COUNT(*) AS DuplicateCount
FROM dbo.FiscalDocuments
GROUP BY CompanyId, DocumentKey
HAVING COUNT(*) > 1;

-- Expected result: the UPDATE must fail with error 51001.
BEGIN TRY
	BEGIN TRANSACTION;
	DECLARE @ClosedId int = (SELECT TOP (1) Id FROM dbo.CashRegisters WHERE IsImmutable = 1);
	IF @ClosedId IS NOT NULL
		UPDATE dbo.CashRegisters SET OpenedAt = OpenedAt WHERE Id = @ClosedId;
	ROLLBACK TRANSACTION;
	PRINT 'WARNING: no immutable cash register was available for the trigger test.';
END TRY
BEGIN CATCH
	IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
	IF ERROR_NUMBER() <> 51001 THROW;
	PRINT 'PASS: closed cash register update was rejected.';
END CATCH;

-- Confirm all business tables with CompanyId use a company index. Review exceptions before go-live.
SELECT t.name AS TableName, c.name AS CompanyColumn, i.name AS IndexName
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'CompanyId'
LEFT JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.column_id = c.column_id
LEFT JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id = ic.index_id
WHERE t.is_ms_shipped = 0
ORDER BY t.name, i.name;
