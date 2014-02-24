INSERT INTO Production.UnitMeasure
VALUES (N'FT2', N'Square Feet ', '20080923'), (N'Y', N'Yards', '20080923'), (N'Y3', N'Cubic Yards', '20080923');
GO
INSERT INTO Production.UnitMeasure (Name, UnitMeasureCode, ModifiedDate)
VALUES (N'Square Yards', N'Y2', GETDATE());
GO