-- ============================================================
-- Script: V001__Create_Database.sql
-- Description: Creates the CentralIdentityDb database if it
--              does not already exist.
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CentralIdentityDb')
BEGIN
    CREATE DATABASE CentralIdentityDb
        COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE CentralIdentityDb;
GO
