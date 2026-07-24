-- ============================================================
-- Migration: Add TicketTypeId column to VenueSectionLayouts
-- Run once against the Eventix database
-- ============================================================

-- 1. Thêm cột TicketTypeId (nullable)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.VenueSectionLayouts')
      AND name = N'TicketTypeId'
)
BEGIN
    ALTER TABLE [dbo].[VenueSectionLayouts]
        ADD [TicketTypeId] [uniqueidentifier] NULL;
    PRINT 'Column TicketTypeId added to VenueSectionLayouts.';
END
ELSE
BEGIN
    PRINT 'Column TicketTypeId already exists.';
END
GO

-- 2. Thêm Foreign Key về TicketTypes (CASCADE DELETE)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_VenueSectionLayouts_TicketTypes'
)
BEGIN
    ALTER TABLE [dbo].[VenueSectionLayouts]
        ADD CONSTRAINT [FK_VenueSectionLayouts_TicketTypes]
        FOREIGN KEY ([TicketTypeId])
        REFERENCES [dbo].[TicketTypes] ([Id])
        ON DELETE CASCADE;
    PRINT 'FK_VenueSectionLayouts_TicketTypes added.';
END
ELSE
BEGIN
    PRINT 'FK already exists.';
END
GO

-- 3. Thêm index
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VenueSectionLayouts')
      AND name = N'IX_VenueSectionLayouts_TicketTypeId'
)
BEGIN
    CREATE INDEX [IX_VenueSectionLayouts_TicketTypeId]
        ON [dbo].[VenueSectionLayouts] ([TicketTypeId]);
    PRINT 'Index IX_VenueSectionLayouts_TicketTypeId added.';
END
GO
