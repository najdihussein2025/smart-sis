-- Smart SIS: create the database (run once in SSMS as a login with dbcreator).
-- Tables are NOT created here. They come from EF Core migrations:
--   cd backend
--   dotnet tool restore
--   dotnet ef database update -p src/SmartSis.Api
-- or run 02_schema.sql (generated from the migrations) in SSMS.

IF DB_ID(N'SmartSis') IS NULL
BEGIN
    CREATE DATABASE [SmartSis];
END
GO

ALTER DATABASE [SmartSis] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

SELECT @@VERSION AS SqlServerVersion; -- paste this into PROJECT.md §11 (decides VECTOR vs varbinary)
GO
