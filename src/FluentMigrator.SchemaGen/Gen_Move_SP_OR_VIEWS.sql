/* 
 * Generates shells script code to MOVE stored procs or views into subdirectories corresponding to their dependency order.
 * Replace 'P' with 'V' to select views, or 'IF' to select functions.
 * Just copy/paste the 1st column into a batch script file.
 */

WITH TablesCTE(ObjType, SchemaName, ObjectName, ObjectID, Ordinal) AS
(
    SELECT  so.type AS ObjType, OBJECT_SCHEMA_NAME(so.object_id) AS SchemaName, OBJECT_NAME(so.object_id) AS ObjectName, so.object_id AS ObjectID, 
	0 AS Ordinal
    FROM sys.objects AS so
    WHERE so.type IN ( 'P'  ) AND  so.is_ms_Shipped = 0

    UNION ALL

    SELECT so.type AS ObjType, OBJECT_SCHEMA_NAME(so.object_id) AS SchemaName, OBJECT_NAME(so.object_id) AS ObjectName, so.object_id AS ObjectID,
	tt.Ordinal + 1 AS Ordinal
    FROM sys.objects AS so
    INNER JOIN sys.sql_expression_dependencies AS dep ON dep.referencing_id = so.object_id 
    INNER JOIN TablesCTE AS tt ON dep.referenced_id = tt.ObjectID
    WHERE so.type IN ( 'P' ) AND so.is_ms_Shipped = 0
)

SELECT DISTINCT 'move ' + t.ObjectName + '.sql D' + CAST(tt.Ordinal as VARCHAR), tt.Ordinal, t.ObjType, t.SchemaName, t.ObjectName, t.ObjectID
    FROM TablesCTE AS t
    INNER JOIN
	(
	    SELECT itt.ObjType, itt.SchemaName AS SchemaName, itt.ObjectName, itt.ObjectID, 
		   Max(itt.Ordinal) AS Ordinal
	    FROM TablesCTE AS itt
	    GROUP BY itt.ObjType, itt.SchemaName, itt.ObjectName, itt.ObjectID
	) AS tt
	ON t.ObjectID = tt.ObjectID AND t.Ordinal = tt.Ordinal
ORDER BY tt.Ordinal, t.ObjType, t.SchemaName, t.ObjectName, t.ObjectID