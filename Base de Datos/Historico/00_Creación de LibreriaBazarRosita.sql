
USE master;
GO

-- Crear la base de datos si todavía no existe.
IF DB_ID(N'LibreriaBazarRosita') IS NULL
BEGIN
    EXEC('CREATE DATABASE [LibreriaBazarRosita]');
END;
GO

-- Seleccionar nuestra base de datos.
USE LibreriaBazarRosita;
GO

-- Verificar la base de datos actual.
SELECT DB_NAME() AS BaseDeDatosActual;
GO
