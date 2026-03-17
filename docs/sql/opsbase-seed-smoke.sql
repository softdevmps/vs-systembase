/* OpsBase - Seed smoke (muy corto, idempotente) */

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[ResourceDefinition] WHERE [Codigo] = 'LAPTOP')
BEGIN
    INSERT INTO [sys_opsbase].[ResourceDefinition] ([Codigo], [Nombre], [Descripcion], [TrackMode], [IsActive])
    VALUES ('LAPTOP', 'Laptop', 'Equipo de prueba', 'serial', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-A1')
BEGIN
    INSERT INTO [sys_opsbase].[Location] ([Codigo], [Nombre], [Tipo], [ParentLocationId], [Capacidad], [IsActive])
    VALUES ('POS-A1', 'Posicion A1', 'posicion', NULL, 100, 1);
END
GO

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
    FROM [sys_opsbase].[StockBalance] stb
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = stb.[ResourceInstanceId]
    INNER JOIN [sys_opsbase].[Location] l ON l.[Id] = stb.[LocationId]
    WHERE ri.[CodigoInterno] = 'LAP-001' AND l.[Codigo] = 'POS-A1'
)
BEGIN
    DECLARE @riLap001Id INT;
    DECLARE @locA1Id INT;
    SELECT @riLap001Id = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'LAP-001';
    SELECT @locA1Id = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-A1';

    INSERT INTO [sys_opsbase].[StockBalance] ([ResourceInstanceId], [LocationId], [StockReal], [StockReservado], [StockDisponible])
    VALUES (@riLap001Id, @locA1Id, 1, 0, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM [sys_opsbase].[Movement] WHERE [ReferenceNo] = 'ING-SMOKE-0001')
BEGIN
    DECLARE @locA1Id2 INT;
    SELECT @locA1Id2 = [Id] FROM [sys_opsbase].[Location] WHERE [Codigo] = 'POS-A1';

    INSERT INTO [sys_opsbase].[Movement]
        ([MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [CreatedBy])
    VALUES
        ('ingreso', 'confirmado', NULL, @locA1Id2, 'ING-SMOKE-0001', 'Ingreso smoke test', 'admin');
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM [sys_opsbase].[MovementLine] ml
    INNER JOIN [sys_opsbase].[Movement] m ON m.[Id] = ml.[MovementId]
    INNER JOIN [sys_opsbase].[ResourceInstance] ri ON ri.[Id] = ml.[ResourceInstanceId]
    WHERE m.[ReferenceNo] = 'ING-SMOKE-0001' AND ri.[CodigoInterno] = 'LAP-001'
)
BEGIN
    DECLARE @movIngId INT;
    DECLARE @riLap001Id2 INT;
    SELECT @movIngId = [Id] FROM [sys_opsbase].[Movement] WHERE [ReferenceNo] = 'ING-SMOKE-0001';
    SELECT @riLap001Id2 = [Id] FROM [sys_opsbase].[ResourceInstance] WHERE [CodigoInterno] = 'LAP-001';

    INSERT INTO [sys_opsbase].[MovementLine] ([MovementId], [ResourceInstanceId], [Quantity], [UnitCost], [Serie], [Lote])
    VALUES (@movIngId, @riLap001Id2, 1, 1000.0000, 'SN-LAP-001', NULL);
END
GO

