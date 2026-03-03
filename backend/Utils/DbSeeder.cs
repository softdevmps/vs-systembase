using Backend.Data;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Utils
{
    public static class DbSeeder
    {
        public static bool EnsureAibaseMetadataOnDemand(SystemBaseContext context, ILogger? logger = null)
        {
            var system = context.Systems
                .AsEnumerable()
                .FirstOrDefault(IsAibaseSystem);
            if (system == null)
                return false;

            return EnsureAibaseMetadataOnDemand(context, system.Id, logger);
        }

        public static bool EnsureAibaseMetadataOnDemand(SystemBaseContext context, int systemId, ILogger? logger = null)
        {
            var log = logger ?? NullLogger.Instance;
            var system = context.Systems.FirstOrDefault(s => s.Id == systemId);
            if (system == null || !IsAibaseSystem(system))
                return false;

            EnsureAibaseCoreSchema(context, log);
            EnsureAibaseDataBridge(context, log);
            EnsureAibaseEntityMetadata(context, system, log);
            return true;
        }

        public static void Seed(IServiceProvider services, ILogger logger)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SystemBaseContext>();

                if (!context.Database.CanConnect())
                {
                    logger.LogWarning("DbSeeder: no se pudo conectar a la base de datos. Se omite el seed.");
                    return;
                }

                EnsureModules(context, logger);
                var adminRole = EnsureAdminRole(context, logger);
                var adminUser = EnsureAdminUser(context, adminRole, logger);
                CleanupLegacyAibaseMenu(context, logger);
                var aibaseSystem = EnsureAibaseSystem(context, logger);
                EnsureAibaseModules(context, aibaseSystem, logger);
                EnsureAibaseSystemMenu(context, aibaseSystem, logger);
                EnsureAibaseCoreSchema(context, logger);
                EnsureAibaseDataBridge(context, logger);
                EnsureAibaseEntityMetadata(context, aibaseSystem, logger);
                var baseMenus = EnsureBaseMenus(context, logger);
                EnsureRoleMenus(context, adminRole, baseMenus, logger);

                if (adminUser != null && adminUser.RolId != adminRole.Id)
                {
                    adminUser.RolId = adminRole.Id;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DbSeeder: error inesperado durante el seed.");
            }
        }

        private static void EnsureModules(SystemBaseContext context, ILogger logger)
        {
            var needed = new[]
            {
                new { Name = "backend", Description = "Generador de backend", Version = "1.0" },
                new { Name = "frontend", Description = "Generador de frontend", Version = "1.0" }
            };

            var existing = context.Modules
                .Select(m => m.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = false;
            foreach (var module in needed)
            {
                if (existing.Contains(module.Name))
                    continue;

                context.Modules.Add(new Modules
                {
                    Name = module.Name,
                    Description = module.Description,
                    Version = module.Version
                });
                added = true;
            }

            if (added)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: modulos base creados.");
            }
        }

        private static Roles EnsureAdminRole(SystemBaseContext context, ILogger logger)
        {
            var role = context.Roles.FirstOrDefault(r => r.Nombre.ToLower() == "admin");
            if (role != null)
                return role;

            role = new Roles
            {
                Nombre = "Admin",
                Activo = true
            };

            context.Roles.Add(role);
            context.SaveChanges();
            logger.LogInformation("DbSeeder: rol Admin creado.");
            return role;
        }

        private static Usuarios? EnsureAdminUser(SystemBaseContext context, Roles adminRole, ILogger logger)
        {
            var user = context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefault(u => u.Username == "admin" || u.Email == "admin@local");

            if (user != null)
                return user;

            var hash = BCrypt.Net.BCrypt.HashPassword("admin");
            user = new Usuarios
            {
                Username = "admin",
                Email = "admin@local",
                PasswordHash = hash,
                Nombre = "Admin",
                Apellido = "System",
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                RolId = adminRole.Id
            };

            context.Usuarios.Add(user);
            context.SaveChanges();
            logger.LogInformation("DbSeeder: usuario admin creado (admin/admin)." );
            return user;
        }

        private static List<Menus> EnsureBaseMenus(SystemBaseContext context, ILogger logger)
        {
            var menus = context.Menus.ToList();
            Menus? sistemaMenu = menus.FirstOrDefault(m => m.Titulo == "Sistema" && m.PadreId == null);
            if (sistemaMenu == null)
            {
                sistemaMenu = new Menus
                {
                    Titulo = "Sistema",
                    Ruta = null,
                    Icono = "mdi-cog",
                    Orden = 2,
                    PadreId = null,
                    Activo = true
                };
                context.Menus.Add(sistemaMenu);
                context.SaveChanges();
                menus.Add(sistemaMenu);
            }

            var baseItems = new[]
            {
                new { Title = "Home", Route = "/home", Icon = "mdi-home", Order = 1, ParentId = (int?)null },
                new { Title = "Sistemas", Route = "/sistemas", Icon = "mdi-apps", Order = 1, ParentId = (int?)sistemaMenu.Id },
                new { Title = "Menu", Route = "/menu", Icon = "mdi-view-list", Order = 2, ParentId = (int?)sistemaMenu.Id },
                new { Title = "Usuarios", Route = "/usuarios", Icon = "mdi-account-group", Order = 3, ParentId = (int?)sistemaMenu.Id },
                new { Title = "Roles", Route = "/roles", Icon = "mdi-shield-account", Order = 4, ParentId = (int?)sistemaMenu.Id }
            };

            var created = false;
            foreach (var item in baseItems)
            {
                var exists = menus.Any(m => m.Titulo == item.Title && m.PadreId == item.ParentId);
                if (exists)
                    continue;

                var menu = new Menus
                {
                    Titulo = item.Title,
                    Ruta = item.Route,
                    Icono = item.Icon,
                    Orden = item.Order,
                    PadreId = item.ParentId,
                    Activo = true
                };

                context.Menus.Add(menu);
                menus.Add(menu);
                created = true;
            }

            if (created)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: menus base creados.");
            }

            return menus.Where(m => m.Titulo is "Home" or "Sistema" or "Sistemas" or "Menu" or "Usuarios" or "Roles").ToList();
        }

        private static Systems EnsureAibaseSystem(SystemBaseContext context, ILogger logger)
        {
            var system = context.Systems
                .AsEnumerable()
                .FirstOrDefault(IsAibaseSystem);
            if (system != null)
            {
                var changed = false;
                if (string.IsNullOrWhiteSpace(system.Name))
                {
                    system.Name = "Sistema AIBase";
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(system.Namespace))
                {
                    system.Namespace = "AIBase";
                    changed = true;
                }

                if (!system.IsActive)
                {
                    system.IsActive = true;
                    changed = true;
                }

                if (!string.Equals(system.Status, "published", StringComparison.OrdinalIgnoreCase))
                {
                    system.Status = "published";
                    system.PublishedAt ??= DateTime.UtcNow;
                    changed = true;
                }

                if (changed)
                {
                    system.UpdatedAt = DateTime.UtcNow;
                    context.SaveChanges();
                    logger.LogInformation("DbSeeder: sistema AIBase actualizado para operacion en ecosystem.");
                }

                return system;
            }

            system = new Systems
            {
                Slug = "aibase",
                Name = "Sistema AIBase",
                Namespace = "AIBase",
                Description = "Plataforma AI metadata-driven separada de la fabrica.",
                Version = "1.0.0",
                Status = "published",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow
            };

            context.Systems.Add(system);
            context.SaveChanges();
            logger.LogInformation("DbSeeder: sistema AIBase registrado en sb.Systems.");
            return system;
        }

        private static bool IsAibaseSystem(Systems system)
        {
            var slug = (system.Slug ?? string.Empty).Trim();
            if (slug.StartsWith("aibase", StringComparison.OrdinalIgnoreCase))
                return true;

            var name = (system.Name ?? string.Empty).Trim();
            return name.Contains("aibase", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureAibaseModules(SystemBaseContext context, Systems system, ILogger logger)
        {
            var moduleIds = context.Modules
                .Where(m => m.Name == "backend" || m.Name == "frontend")
                .Select(m => new { m.Id, m.Name })
                .ToList();

            var changed = false;
            foreach (var module in moduleIds)
            {
                var existing = context.SystemModules.FirstOrDefault(sm => sm.SystemId == system.Id && sm.ModuleId == module.Id);
                if (existing == null)
                {
                    context.SystemModules.Add(new SystemModules
                    {
                        SystemId = system.Id,
                        ModuleId = module.Id,
                        IsEnabled = true,
                        ConfigJson = "{}"
                    });
                    changed = true;
                    continue;
                }

                if (!existing.IsEnabled)
                {
                    existing.IsEnabled = true;
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: modulos backend/frontend habilitados para AIBase.");
            }
        }

        private static void EnsureAibaseSystemMenu(SystemBaseContext context, Systems system, ILogger logger)
        {
            var exists = context.SystemMenus.Any(m => m.SystemId == system.Id);
            if (exists)
                return;

            context.SystemMenus.Add(new SystemMenus
            {
                SystemId = system.Id,
                ParentId = null,
                Title = "AIBase",
                Route = "/s/aibase",
                Icon = "mdi-brain",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            context.SaveChanges();
            logger.LogInformation("DbSeeder: menu base del sistema AIBase creado en sb.SystemMenus.");
        }

        private static void EnsureAibaseCoreSchema(SystemBaseContext context, ILogger logger)
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'sb_ai')
    EXEC('CREATE SCHEMA sb_ai');

IF OBJECT_ID('sb_ai.Templates', 'U') IS NULL
BEGIN
    CREATE TABLE sb_ai.Templates
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key] NVARCHAR(80) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        PipelineJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_sb_ai_Templates_PipelineJson DEFAULT('{}'),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_ai_Templates_IsActive DEFAULT(1),
        [Version] NVARCHAR(20) NOT NULL CONSTRAINT DF_sb_ai_Templates_Version DEFAULT('1.0'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_ai_Templates_CreatedAt DEFAULT(SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UX_sb_ai_Templates_Key UNIQUE ([Key])
    );
END;

IF OBJECT_ID('sb_ai.Projects', 'U') IS NULL
BEGIN
    CREATE TABLE sb_ai.Projects
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Slug NVARCHAR(80) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Language] NVARCHAR(10) NOT NULL CONSTRAINT DF_sb_ai_Projects_Language DEFAULT('es'),
        Tone NVARCHAR(100) NULL,
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_ai_Projects_Status DEFAULT('draft'),
        IsActive BIT NOT NULL CONSTRAINT DF_sb_ai_Projects_IsActive DEFAULT(1),
        TemplateId INT NOT NULL,
        TenantId INT NULL,
        CreatedByUserId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_ai_Projects_CreatedAt DEFAULT(SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UX_sb_ai_Projects_Slug UNIQUE (Slug),
        CONSTRAINT FK_sb_ai_Projects_Template FOREIGN KEY (TemplateId) REFERENCES sb_ai.Templates(Id)
    );
END;

IF OBJECT_ID('sb_ai.Runs', 'U') IS NULL
BEGIN
    CREATE TABLE sb_ai.Runs
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProjectId INT NOT NULL,
        RunType NVARCHAR(50) NOT NULL CONSTRAINT DF_sb_ai_Runs_RunType DEFAULT('dataset_build'),
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_ai_Runs_Status DEFAULT('queued'),
        EngineRunId NVARCHAR(120) NULL,
        ProgressPct INT NOT NULL CONSTRAINT DF_sb_ai_Runs_ProgressPct DEFAULT(0),
        RequestedByUserId INT NOT NULL,
        TriggerSource NVARCHAR(30) NOT NULL CONSTRAINT DF_sb_ai_Runs_TriggerSource DEFAULT('manual'),
        InputJson NVARCHAR(MAX) NULL,
        OutputJson NVARCHAR(MAX) NULL,
        LastError NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_sb_ai_Runs_CreatedAt DEFAULT(SYSDATETIME()),
        StartedAt DATETIME2 NULL,
        FinishedAt DATETIME2 NULL,
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_sb_ai_Runs_Project FOREIGN KEY (ProjectId) REFERENCES sb_ai.Projects(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sb_ai.Templates WHERE [Key] = 'extractor-json')
BEGIN
    INSERT INTO sb_ai.Templates ([Key], [Name], [Description], PipelineJson, IsActive, [Version])
    VALUES
    (
        'extractor-json',
        'Extractor JSON',
        'Extrae estructura JSON validada por schema.',
        '{""key"":""extractor-json"",""steps"":[{""name"":""dataset_build"",""engine"":""/dataset/build""},{""name"":""train_lora"",""engine"":""/train/lora""},{""name"":""eval"",""engine"":""/eval/run""}],""supports"":[""versioning"",""rollback""]}',
        1,
        '1.0'
    );
END;

IF NOT EXISTS (SELECT 1 FROM sb_ai.Templates WHERE [Key] = 'chat-rag')
BEGIN
    INSERT INTO sb_ai.Templates ([Key], [Name], [Description], PipelineJson, IsActive, [Version])
    VALUES
    (
        'chat-rag',
        'Chat RAG',
        'Chat con contexto documental e indice vectorial.',
        '{""key"":""chat-rag"",""steps"":[{""name"":""dataset_build"",""engine"":""/dataset/build""},{""name"":""rag_index"",""engine"":""/rag/index""},{""name"":""eval"",""engine"":""/eval/run""}],""supports"":[""versioning"",""rollback""]}',
        1,
        '1.0'
    );
END;
";

            var sqlEscaped = sql.Replace("{", "{{").Replace("}", "}}");
            context.Database.ExecuteSqlRaw(sqlEscaped);
            logger.LogInformation("DbSeeder: schema core AIBase (sb_ai) validado.");
        }

        private static void EnsureAibaseDataBridge(SystemBaseContext context, ILogger logger)
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'sys_aibase')
    EXEC('CREATE SCHEMA sys_aibase');

IF OBJECT_ID('sb_ai.Templates', 'U') IS NOT NULL
   AND OBJECT_ID('sys_aibase.Templates', 'U') IS NULL
   AND OBJECT_ID('sys_aibase.Templates', 'SN') IS NULL
BEGIN
    EXEC('CREATE SYNONYM sys_aibase.Templates FOR sb_ai.Templates');
END;

IF OBJECT_ID('sb_ai.Projects', 'U') IS NOT NULL
   AND OBJECT_ID('sys_aibase.Projects', 'U') IS NULL
   AND OBJECT_ID('sys_aibase.Projects', 'SN') IS NULL
BEGIN
    EXEC('CREATE SYNONYM sys_aibase.Projects FOR sb_ai.Projects');
END;

IF OBJECT_ID('sb_ai.Runs', 'U') IS NOT NULL
   AND OBJECT_ID('sys_aibase.Runs', 'U') IS NULL
   AND OBJECT_ID('sys_aibase.Runs', 'SN') IS NULL
BEGIN
    EXEC('CREATE SYNONYM sys_aibase.Runs FOR sb_ai.Runs');
END;
";

            context.Database.ExecuteSqlRaw(sql);
            logger.LogInformation("DbSeeder: bridge de datos AIBase validado (sys_aibase -> sb_ai).");
        }

        private static void EnsureAibaseEntityMetadata(SystemBaseContext context, Systems system, ILogger logger)
        {
            var definitions = BuildAibaseEntityDefinitions();
            var changed = false;

            foreach (var def in definitions)
            {
                var entity = context.Entities
                    .Include(e => e.Fields)
                    .FirstOrDefault(e => e.SystemId == system.Id && e.Name == def.Name);

                if (entity == null)
                {
                    entity = new Entities
                    {
                        SystemId = system.Id,
                        Name = def.Name,
                        DisplayName = def.DisplayName,
                        TableName = def.TableName,
                        Description = def.Description,
                        IsActive = true,
                        SortOrder = def.SortOrder,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Entities.Add(entity);
                    context.SaveChanges();
                    changed = true;
                }

                if (entity.TableName != def.TableName || entity.DisplayName != def.DisplayName || entity.Description != def.Description || entity.SortOrder != def.SortOrder || !entity.IsActive)
                {
                    entity.TableName = def.TableName;
                    entity.DisplayName = def.DisplayName;
                    entity.Description = def.Description;
                    entity.SortOrder = def.SortOrder;
                    entity.IsActive = true;
                    entity.UpdatedAt = DateTime.UtcNow;
                    changed = true;
                }

                foreach (var fieldDef in def.Fields)
                {
                    var field = entity.Fields.FirstOrDefault(f => f.Name == fieldDef.Name || f.ColumnName == fieldDef.ColumnName);
                    if (field == null)
                    {
                        field = new Fields
                        {
                            EntityId = entity.Id,
                            Name = fieldDef.Name,
                            ColumnName = fieldDef.ColumnName,
                            DataType = fieldDef.DataType,
                            Required = fieldDef.Required,
                            MaxLength = fieldDef.MaxLength,
                            Precision = fieldDef.Precision,
                            Scale = fieldDef.Scale,
                            DefaultValue = fieldDef.DefaultValue,
                            IsPrimaryKey = fieldDef.IsPrimaryKey,
                            IsIdentity = fieldDef.IsIdentity,
                            IsUnique = fieldDef.IsUnique,
                            SortOrder = fieldDef.SortOrder,
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Fields.Add(field);
                        changed = true;
                        continue;
                    }

                    if (field.Name != fieldDef.Name ||
                        field.ColumnName != fieldDef.ColumnName ||
                        field.DataType != fieldDef.DataType ||
                        field.Required != fieldDef.Required ||
                        field.MaxLength != fieldDef.MaxLength ||
                        field.Precision != fieldDef.Precision ||
                        field.Scale != fieldDef.Scale ||
                        field.DefaultValue != fieldDef.DefaultValue ||
                        field.IsPrimaryKey != fieldDef.IsPrimaryKey ||
                        field.IsIdentity != fieldDef.IsIdentity ||
                        field.IsUnique != fieldDef.IsUnique ||
                        field.SortOrder != fieldDef.SortOrder)
                    {
                        field.Name = fieldDef.Name;
                        field.ColumnName = fieldDef.ColumnName;
                        field.DataType = fieldDef.DataType;
                        field.Required = fieldDef.Required;
                        field.MaxLength = fieldDef.MaxLength;
                        field.Precision = fieldDef.Precision;
                        field.Scale = fieldDef.Scale;
                        field.DefaultValue = fieldDef.DefaultValue;
                        field.IsPrimaryKey = fieldDef.IsPrimaryKey;
                        field.IsIdentity = fieldDef.IsIdentity;
                        field.IsUnique = fieldDef.IsUnique;
                        field.SortOrder = fieldDef.SortOrder;
                        field.UpdatedAt = DateTime.UtcNow;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: metadata de entidades AIBase lista para modulo Datos.");
            }
        }

        private static List<AibaseEntityDefinition> BuildAibaseEntityDefinitions()
        {
            return new List<AibaseEntityDefinition>
            {
                new AibaseEntityDefinition
                {
                    Name = "Templates",
                    DisplayName = "Templates",
                    TableName = "Templates",
                    Description = "Plantillas de pipelines AIBase.",
                    SortOrder = 1,
                    Fields = new List<AibaseFieldDefinition>
                    {
                        new("Id","Id","int",true,null,null,null,null,true,true,false,1),
                        new("Key","Key","string",true,80,null,null,null,false,false,true,2),
                        new("Name","Name","string",true,200,null,null,null,false,false,false,3),
                        new("Description","Description","string",false,500,null,null,null,false,false,false,4),
                        new("PipelineJson","PipelineJson","string",true,null,null,null,"{}",false,false,false,5),
                        new("IsActive","IsActive","bool",true,null,null,null,"1",false,false,false,6),
                        new("Version","Version","string",true,20,null,null,"1.0",false,false,false,7),
                        new("CreatedAt","CreatedAt","datetime",true,null,null,null,null,false,false,false,8),
                        new("UpdatedAt","UpdatedAt","datetime",false,null,null,null,null,false,false,false,9)
                    }
                },
                new AibaseEntityDefinition
                {
                    Name = "Projects",
                    DisplayName = "Proyectos",
                    TableName = "Projects",
                    Description = "Proyectos AIBase creados desde templates.",
                    SortOrder = 2,
                    Fields = new List<AibaseFieldDefinition>
                    {
                        new("Id","Id","int",true,null,null,null,null,true,true,false,1),
                        new("Slug","Slug","string",true,80,null,null,null,false,false,true,2),
                        new("Name","Name","string",true,200,null,null,null,false,false,false,3),
                        new("Description","Description","string",false,500,null,null,null,false,false,false,4),
                        new("Language","Language","string",true,10,null,null,"es",false,false,false,5),
                        new("Tone","Tone","string",false,100,null,null,null,false,false,false,6),
                        new("Status","Status","string",true,30,null,null,"draft",false,false,false,7),
                        new("IsActive","IsActive","bool",true,null,null,null,"1",false,false,false,8),
                        new("TemplateId","TemplateId","int",true,null,null,null,null,false,false,false,9),
                        new("TenantId","TenantId","int",false,null,null,null,null,false,false,false,10),
                        new("CreatedByUserId","CreatedByUserId","int",true,null,null,null,null,false,false,false,11),
                        new("CreatedAt","CreatedAt","datetime",true,null,null,null,null,false,false,false,12),
                        new("UpdatedAt","UpdatedAt","datetime",false,null,null,null,null,false,false,false,13)
                    }
                },
                new AibaseEntityDefinition
                {
                    Name = "Runs",
                    DisplayName = "Runs",
                    TableName = "Runs",
                    Description = "Ejecuciones de pipelines AIBase.",
                    SortOrder = 3,
                    Fields = new List<AibaseFieldDefinition>
                    {
                        new("Id","Id","int",true,null,null,null,null,true,true,false,1),
                        new("ProjectId","ProjectId","int",true,null,null,null,null,false,false,false,2),
                        new("RunType","RunType","string",true,50,null,null,"dataset_build",false,false,false,3),
                        new("Status","Status","string",true,30,null,null,"queued",false,false,false,4),
                        new("EngineRunId","EngineRunId","string",false,120,null,null,null,false,false,false,5),
                        new("ProgressPct","ProgressPct","int",true,null,null,null,"0",false,false,false,6),
                        new("RequestedByUserId","RequestedByUserId","int",true,null,null,null,null,false,false,false,7),
                        new("TriggerSource","TriggerSource","string",true,30,null,null,"manual",false,false,false,8),
                        new("InputJson","InputJson","string",false,null,null,null,null,false,false,false,9),
                        new("OutputJson","OutputJson","string",false,null,null,null,null,false,false,false,10),
                        new("LastError","LastError","string",false,1000,null,null,null,false,false,false,11),
                        new("CreatedAt","CreatedAt","datetime",true,null,null,null,null,false,false,false,12),
                        new("StartedAt","StartedAt","datetime",false,null,null,null,null,false,false,false,13),
                        new("FinishedAt","FinishedAt","datetime",false,null,null,null,null,false,false,false,14),
                        new("UpdatedAt","UpdatedAt","datetime",false,null,null,null,null,false,false,false,15)
                    }
                }
            };
        }

        private sealed class AibaseEntityDefinition
        {
            public string Name { get; init; } = "";
            public string DisplayName { get; init; } = "";
            public string TableName { get; init; } = "";
            public string Description { get; init; } = "";
            public int SortOrder { get; init; }
            public List<AibaseFieldDefinition> Fields { get; init; } = new();
        }

        private sealed class AibaseFieldDefinition
        {
            public AibaseFieldDefinition(
                string name,
                string columnName,
                string dataType,
                bool required,
                int? maxLength,
                int? precision,
                int? scale,
                string? defaultValue,
                bool isPrimaryKey,
                bool isIdentity,
                bool isUnique,
                int sortOrder)
            {
                Name = name;
                ColumnName = columnName;
                DataType = dataType;
                Required = required;
                MaxLength = maxLength;
                Precision = precision;
                Scale = scale;
                DefaultValue = defaultValue;
                IsPrimaryKey = isPrimaryKey;
                IsIdentity = isIdentity;
                IsUnique = isUnique;
                SortOrder = sortOrder;
            }

            public string Name { get; }
            public string ColumnName { get; }
            public string DataType { get; }
            public bool Required { get; }
            public int? MaxLength { get; }
            public int? Precision { get; }
            public int? Scale { get; }
            public string? DefaultValue { get; }
            public bool IsPrimaryKey { get; }
            public bool IsIdentity { get; }
            public bool IsUnique { get; }
            public int SortOrder { get; }
        }

        private static void CleanupLegacyAibaseMenu(SystemBaseContext context, ILogger logger)
        {
            var legacyMenus = context.Menus
                .Where(m => m.Titulo == "AIBase" || m.Ruta == "/aibase")
                .ToList();

            if (!legacyMenus.Any())
                return;

            var roles = context.Roles
                .Include(r => r.Menu)
                .ToList();

            foreach (var menu in legacyMenus)
            {
                foreach (var role in roles.Where(r => r.Menu.Any(m => m.Id == menu.Id)))
                {
                    role.Menu.Remove(menu);
                }
            }

            context.Menus.RemoveRange(legacyMenus);
            context.SaveChanges();
            logger.LogInformation("DbSeeder: se removieron {Count} menus legacy de AIBase en SystemBase raiz.", legacyMenus.Count);
        }

        private static void EnsureRoleMenus(SystemBaseContext context, Roles role, List<Menus> menus, ILogger logger)
        {
            context.Entry(role).Collection(r => r.Menu).Load();
            var assigned = role.Menu.Select(m => m.Id).ToHashSet();

            var added = false;
            foreach (var menu in menus)
            {
                if (assigned.Contains(menu.Id))
                    continue;
                role.Menu.Add(menu);
                added = true;
            }

            if (added)
            {
                context.SaveChanges();
                logger.LogInformation("DbSeeder: menus asignados al rol Admin.");
            }
        }
    }
}
