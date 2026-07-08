-- Backfill script: compute SHA256(Token) into TokenHash for existing rows
-- Run during maintenance window. This script does NOT remove plaintext Token values.
BEGIN TRANSACTION;

UPDATE RefreshTokens
SET TokenHash = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', Token), 2))
WHERE TokenHash IS NULL AND Token IS NOT NULL AND Token <> '';

COMMIT;

-- Optional: verify rows updated
SELECT COUNT(*) AS BackfilledCount FROM RefreshTokens WHERE TokenHash IS NOT NULL;
