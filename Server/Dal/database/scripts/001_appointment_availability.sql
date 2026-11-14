-- 001: Appointment slots can exist without a customer and carry an availability flag.
-- Required by the appointments feature (Dal.Models.Appointment: CustomerId nullable, IsAvailable).
-- Idempotent: safe to run more than once.

IF COL_LENGTH('dbo.Appointment', 'IsAvailable') IS NULL
BEGIN
    ALTER TABLE dbo.Appointment ADD IsAvailable BIT NULL;
END
GO

-- Existing rows already belong to a customer, so they are booked.
UPDATE dbo.Appointment SET IsAvailable = 0 WHERE IsAvailable IS NULL AND customerId IS NOT NULL;
UPDATE dbo.Appointment SET IsAvailable = 1 WHERE IsAvailable IS NULL AND customerId IS NULL;
GO

-- A free slot has no customer.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.Appointment') AND name = 'customerId' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.Appointment ALTER COLUMN customerId VARCHAR(9) NULL;
END
GO
