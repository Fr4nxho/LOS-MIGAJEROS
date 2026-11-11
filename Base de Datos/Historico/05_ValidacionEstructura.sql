
USE [LibreriaBazarRosita];
GO

-- =========================================
-- VERIFICAR LA ESTRUCTURA GENERAL
-- =========================================

SELECT
    (SELECT COUNT(*)
     FROM sys.tables
     WHERE schema_id = SCHEMA_ID('dbo')
       AND is_ms_shipped = 0
    ) AS TotalTablas,

    (SELECT COUNT(*)
     FROM sys.key_constraints
     WHERE type = 'PK'
       AND OBJECT_SCHEMA_NAME(parent_object_id) = 'dbo'
    ) AS TotalClavesPrimarias,

    (SELECT COUNT(*)
     FROM sys.foreign_keys
     WHERE OBJECT_SCHEMA_NAME(parent_object_id) = 'dbo'
    ) AS TotalClavesForaneas,

    (SELECT COUNT(*)
     FROM sys.foreign_keys
     WHERE OBJECT_SCHEMA_NAME(parent_object_id) = 'dbo'
       AND (is_disabled = 1 OR is_not_trusted = 1)
    ) AS RelacionesConProblemas;
GO



USE [LibreriaBazarRosita];
GO

-- =========================================
-- MOSTRAR LAS 16 CLAVES FORANEAS
-- =========================================

SELECT
    fk.name AS NombreClaveForanea,
    OBJECT_NAME(fk.parent_object_id) AS TablaOrigen,
    OBJECT_NAME(fk.referenced_object_id) AS TablaDestino,
    CASE
        WHEN fk.is_disabled = 0
         AND fk.is_not_trusted = 0
        THEN 'CORRECTA'
        ELSE 'REVISAR'
    END AS EstadoRelacion
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = 'dbo'
ORDER BY TablaOrigen, NombreClaveForanea;
GO
