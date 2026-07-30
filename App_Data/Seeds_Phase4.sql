-- =============================================================================
-- EidUbahle Enterprise ERP – Phase 4 Seed Data
-- Inventory Permissions, Default Units of Measure, Sample Categories
-- Run after Schema_Phase4.sql.
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Inventory Permissions
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO sys_Permissions(Id, Module, Feature, Action, PermissionKey)
SELECT NEWID(), p.Module, p.Feature, p.Action, p.PermissionKey
FROM (VALUES
  -- Products
  ('Inventory','Product','View',    'inventory.product.view'),
  ('Inventory','Product','Create',  'inventory.product.create'),
  ('Inventory','Product','Edit',    'inventory.product.edit'),
  ('Inventory','Product','Delete',  'inventory.product.delete'),
  ('Inventory','Product','Import',  'inventory.product.import'),
  ('Inventory','Product','Export',  'inventory.product.export'),
  -- Categories
  ('Inventory','Category','View',   'inventory.category.view'),
  ('Inventory','Category','Create', 'inventory.category.create'),
  ('Inventory','Category','Edit',   'inventory.category.edit'),
  ('Inventory','Category','Delete', 'inventory.category.delete'),
  -- Brands
  ('Inventory','Brand','View',      'inventory.brand.view'),
  ('Inventory','Brand','Create',    'inventory.brand.create'),
  ('Inventory','Brand','Edit',      'inventory.brand.edit'),
  ('Inventory','Brand','Delete',    'inventory.brand.delete'),
  -- Units of Measure
  ('Inventory','UOM','View',        'inventory.uom.view'),
  ('Inventory','UOM','Create',      'inventory.uom.create'),
  ('Inventory','UOM','Edit',        'inventory.uom.edit'),
  ('Inventory','UOM','Delete',      'inventory.uom.delete'),
  -- Warehouses
  ('Inventory','Warehouse','View',  'inventory.warehouse.view'),
  ('Inventory','Warehouse','Create','inventory.warehouse.create'),
  ('Inventory','Warehouse','Edit',  'inventory.warehouse.edit'),
  ('Inventory','Warehouse','Delete','inventory.warehouse.delete'),
  -- Stock Movements (Adjustments, Opening, Transfers)
  ('Inventory','StockMovement','View',   'inventory.movement.view'),
  ('Inventory','StockMovement','Create', 'inventory.movement.create'),
  ('Inventory','StockMovement','Edit',   'inventory.movement.edit'),
  ('Inventory','StockMovement','Delete', 'inventory.movement.delete'),
  ('Inventory','StockMovement','Post',   'inventory.movement.post'),
  -- Batches & Serials
  ('Inventory','Batch','View',      'inventory.batch.view'),
  ('Inventory','Batch','Create',    'inventory.batch.create'),
  ('Inventory','Batch','Edit',      'inventory.batch.edit'),
  -- Reservations
  ('Inventory','Reservation','View',    'inventory.reservation.view'),
  ('Inventory','Reservation','Create',  'inventory.reservation.create'),
  ('Inventory','Reservation','Release', 'inventory.reservation.release'),
  -- Valuation & Reports
  ('Inventory','Valuation','View',  'inventory.valuation.view'),
  -- Alerts
  ('Inventory','Alert','View',      'inventory.alert.view'),
  ('Inventory','Alert','Acknowledge','inventory.alert.acknowledge')
) AS p(Module, Feature, Action, PermissionKey)
WHERE NOT EXISTS (
    SELECT 1 FROM sys_Permissions WHERE PermissionKey = p.PermissionKey
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Assign inventory permissions to system roles
--    (Adjust per-tenant after seeding; sys_Roles is per-tenant so we skip here)
-- ─────────────────────────────────────────────────────────────────────────────
-- NOTE: Role-permission assignment is done at runtime when creating new tenants,
-- as sys_Roles is tenant-scoped. The application seeds roles on tenant creation.
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Global/system UOM seeds
--    These are seeded per-tenant in the application on first run.
--    Below we document the canonical defaults for reference.
-- ─────────────────────────────────────────────────────────────────────────────
-- Count-based (base: each)
--   Piece  (ea)   = 1 each
--   Dozen  (dz)   = 12 each
--   Box    (box)  – user-defined conversion
--   Carton (ctn)  – user-defined conversion
--   Pallet (plt)  – user-defined conversion
-- Weight (base: kg)
--   Kilogram (kg)  = 1 kg
--   Gram     (g)   = 0.001 kg
--   Pound    (lb)  = 0.453592 kg
--   Tonne    (t)   = 1000 kg
-- Volume (base: L)
--   Litre    (L)   = 1 L
--   Millilitre (mL) = 0.001 L
--   Gallon   (gal) = 3.78541 L
-- Length (base: m)
--   Metre    (m)   = 1 m
--   Centimetre (cm) = 0.01 m
--   Foot     (ft)  = 0.3048 m
-- These are inserted by application code (InventoryService.SeedDefaultUoms) per tenant.
GO
