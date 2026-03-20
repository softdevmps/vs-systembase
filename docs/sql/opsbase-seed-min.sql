/* OpsBase - Seed corto para pruebas funcionales (idempotente) */

/* 1) ResourceDefinition */
IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'LAPTOP')
BEGIN
    INSERT INTO [sys_opsbase].[ResourceDefinition] ([Codigo], [Nombre], [Descripcion], [TrackMode], [IsActive])
    VALUES ('LAPTOP', 'Laptop', 'Equipo portatil', 'serial', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'MOUSE')
BEGIN
    INSERT INTO [sys_opsbase].[ResourceDefinition] ([Codigo], [Nombre], [Descripcion], [TrackMode], [IsActive])
    VALUES ('MOUSE', 'Mouse', 'Periferico apuntador', 'none', 1);
END
GO

/* 2) Location */
IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Location] WHERE [Codigo] = 'DEP-CENTRAL')
BEGIN
    INSERT INTO [sys_opsbase].[Location] ([Codigo], [Nombre], [Tipo], [ParentLocationId], [Capacidad], [IsActive])
    VALUES ('DEP-CENTRAL', 'Deposito Central', 'deposito', NULL, 1000, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-A1')
BEGIN
    DECLARE @depCentralId INT;
    SELECT @depCentralId = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'DEP-CENTRAL';

    INSERT INTO [sys_opsbase].[Location] ([Codigo], [Nombre], [Tipo], [ParentLocationId], [Capacidad], [IsActive])
    VALUES ('POS-A1', 'Posicion A1', 'posicion', @depCentralId, 100, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-B1')
BEGIN
    DECLARE @depCentralId2 INT;
    SELECT @depCentralId2 = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'DEP-CENTRAL';

    INSERT INTO [sys_opsbase].[Location] ([Codigo], [Nombre], [Tipo], [ParentLocationId], [Capacidad], [IsActive])
    VALUES ('POS-B1', 'Posicion B1', 'posicion', @depCentralId2, 100, 1);
END
GO

/* 3) ResourceInstance */
IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[ResourceInstance] ri
    INNER JOIN [sys_opsbase].[ResourceDefinition] rd ON rd.[Id] = ri.[ResourceDefinitionId]
    WHERE rd.[Codigo] = 'LAPTOP' AND ri.[CodigoInterno] = 'LAP-001'
)
BEGIN
    DECLARE @rdLaptopId INT;
    SELECT @rdLaptopId = [Id] FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'LAPTOP';

    INSERT INTO [sys_opsbase].[ResourceInstance] ([ResourceDefinitionId], [CodigoInterno], [Estado], [Serie], [Lote], [Vencimiento], [IsActive])
    VALUES (@rdLaptopId, 'LAP-001', 'activo', 'SN-LAP-001', NULL, NULL, 1);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[ResourceInstance] ri
    INNER JOIN [sys_opsbase].[ResourceDefinition] rd ON rd.[Id] = ri.[ResourceDefinitionId]
    WHERE rd.[Codigo] = 'LAPTOP' AND ri.[CodigoInterno] = 'LAP-002'
)
BEGIN
    DECLARE @rdLaptopId2 INT;
    SELECT @rdLaptopId2 = [Id] FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'LAPTOP';

    INSERT INTO [sys_opsbase].[ResourceInstance] ([ResourceDefinitionId], [CodigoInterno], [Estado], [Serie], [Lote], [Vencimiento], [IsActive])
    VALUES (@rdLaptopId2, 'LAP-002', 'activo', 'SN-LAP-002', NULL, NULL, 1);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[ResourceInstance] ri
    INNER JOIN [sys_opsbase].[ResourceDefinition] rd ON rd.[Id] = ri.[ResourceDefinitionId]
    WHERE rd.[Codigo] = 'MOUSE' AND ri.[CodigoInterno] = 'MOU-001'
)
BEGIN
    DECLARE @rdMouseId INT;
    SELECT @rdMouseId = [Id] FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'MOUSE';

    INSERT INTO [sys_opsbase].[ResourceInstance] ([ResourceDefinitionId], [CodigoInterno], [Estado], [Serie], [Lote], [Vencimiento], [IsActive])
    VALUES (@rdMouseId, 'MOU-001', 'activo', NULL, NULL, NULL, 1);
END
GO

/* 4) AttributeDefinition */
IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[AttributeDefinition] ad
    INNER JOIN [sys_opsbase].[ResourceDefinition] rd ON rd.[Id] = ad.[ResourceDefinitionId]
    WHERE rd.[Codigo] = 'LAPTOP' AND ad.[Codigo] = 'MARCA'
)
BEGIN
    DECLARE @rdLaptopId3 INT;
    SELECT @rdLaptopId3 = [Id] FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'LAPTOP';

    INSERT INTO [sys_opsbase].[AttributeDefinition]
        ([ResourceDefinitionId], [Codigo], [Nombre], [DataType], [IsRequired], [MaxLength], [SortOrder], [IsActive])
    VALUES
        (@rdLaptopId3, 'MARCA', 'Marca', 'string', 1, 100, 1, 1);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[AttributeDefinition] ad
    INNER JOIN [sys_opsbase].[ResourceDefinition] rd ON rd.[Id] = ad.[ResourceDefinitionId]
    WHERE rd.[Codigo] = 'LAPTOP' AND ad.[Codigo] = 'RAM_GB'
)
BEGIN
    DECLARE @rdLaptopId4 INT;
    SELECT @rdLaptopId4 = [Id] FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'LAPTOP';

    INSERT INTO [sys_opsbase].[AttributeDefinition]
        ([ResourceDefinitionId], [Codigo], [Nombre], [DataType], [IsRequired], [MaxLength], [SortOrder], [IsActive])
    VALUES
        (@rdLaptopId4, 'RAM_GB', 'RAM (GB)', 'int', 0, NULL, 2, 1);
END
GO

/* 5) AttributeValue */
IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[AttributeValue] av
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = av.[ResourceInstanceId]
    INNER JOIN [sys_opsbase].[AttributeDefinition] ad ON ad.[Id] = av.[AttributeDefinitionId]
    WHERE ri.[CodigoInterno] = 'LAP-001' AND ad.[Codigo] = 'MARCA'
)
BEGIN
    DECLARE @riLap001Id INT;
    DECLARE @adMarcaId INT;
    SELECT @riLap001Id = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'LAP-001';
    SELECT @adMarcaId = [Id] FROM [sys_opsbase].[AttributeDefinition] WHERE [Codigo] = 'MARCA';

    INSERT INTO [sys_opsbase].[AttributeValue]
        ([ResourceInstanceId], [AttributeDefinitionId], [ValueString], [ValueNumber], [ValueDateTime], [ValueBool], [ValueJson])
    VALUES
        (@riLap001Id, @adMarcaId, 'Lenovo', NULL, NULL, NULL, NULL);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[AttributeValue] av
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = av.[ResourceInstanceId]
    INNER JOIN [sys_opsbase].[AttributeDefinition] ad ON ad.[Id] = av.[AttributeDefinitionId]
    WHERE ri.[CodigoInterno] = 'LAP-001' AND ad.[Codigo] = 'RAM_GB'
)
BEGIN
    DECLARE @riLap001Id2 INT;
    DECLARE @adRamId INT;
    SELECT @riLap001Id2 = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'LAP-001';
    SELECT @adRamId = [Id] FROM [sys_opsbase].[AttributeDefinition] WHERE [Codigo] = 'RAM_GB';

    INSERT INTO [sys_opsbase].[AttributeValue]
        ([ResourceInstanceId], [AttributeDefinitionId], [ValueString], [ValueNumber], [ValueDateTime], [ValueBool], [ValueJson])
    VALUES
        (@riLap001Id2, @adRamId, NULL, 16, NULL, NULL, NULL);
END
GO

/* 6) StockBalance */
IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[StockBalance] stb
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = stb.[ResourceInstanceId]
    INNER JOIN [sys_opsbase].[Location] l ON l.[Id] = stb.[LocationId]
    WHERE ri.[CodigoInterno] = 'LAP-001' AND l.[Codigo] = 'POS-A1'
)
BEGIN
    DECLARE @riLap001Id3 INT;
    DECLARE @locA1Id INT;
    SELECT @riLap001Id3 = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'LAP-001';
    SELECT @locA1Id = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-A1';

    INSERT INTO [sys_opsbase].[StockBalance] ([ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible])
    VALUES (@riLap001Id3, @locA1Id, 1, 0, 1);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[StockBalance] stb
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = stb.[ResourceInstanceId]
    INNER JOIN [sys_opsbase].[Location] l ON l.[Id] = stb.[LocationId]
    WHERE ri.[CodigoInterno] = 'LAP-002' AND l.[Codigo] = 'POS-B1'
)
BEGIN
    DECLARE @riLap002Id INT;
    DECLARE @locB1Id INT;
    SELECT @riLap002Id = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'LAP-002';
    SELECT @locB1Id = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-B1';

    INSERT INTO [sys_opsbase].[StockBalance] ([ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible])
    VALUES (@riLap002Id, @locB1Id, 1, 0, 1);
END
GO

/* 7) Movement + MovementLine */
IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Movement] WHERE [ReferenceNo] = 'ING-OPS-0001')
BEGIN
    DECLARE @locA1Id2 INT;
    SELECT @locA1Id2 = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-A1';

    INSERT INTO [sys_opsbase].[Movement]
        ([MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [CreatedBy])
    VALUES
        ('ingreso', 'confirmado', NULL, @locA1Id2, 'ING-OPS-0001', 'Ingreso inicial de prueba', 'admin');
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[MovementLine] ml
    INNER JOIN [sys_opsbase].[Movement] m ON m.[Id] = ml.[MovementId]
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = ml.[ResourceInstanceId]
    WHERE m.[ReferenceNo] = 'ING-OPS-0001' AND ri.[CodigoInterno] = 'MOU-001'
)
BEGIN
    DECLARE @movIngId INT;
    DECLARE @riMouseId INT;
    SELECT @movIngId = [Id] FROM [sys_opsbase].[Movement] WHERE [ReferenceNo] = 'ING-OPS-0001';
    SELECT @riMouseId = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'MOU-001';

    INSERT INTO [sys_opsbase].[MovementLine] ([MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote])
    VALUES (@movIngId, @riMouseId, 5, 10.0000, NULL, NULL);
END
GO

/* 8) OperationAudit */
IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[OperationAudit]
    WHERE [OperationName] = 'seed'
      AND [EntityName] = 'bootstrap'
      AND [Message] = 'Carga base inicial OpsBase'
)
BEGIN
    INSERT INTO [sys_opsbase].[OperationAudit]
        ([OperationName], [EntityName], [EntityId], [Result], [Message], [Actor], [PayloadJson])
    VALUES
        ('seed', 'bootstrap', NULL, 'ok', 'Carga base inicial OpsBase', 'admin', '{"seed":"opsbase-min"}');
END
GO
