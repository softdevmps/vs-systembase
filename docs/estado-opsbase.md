# Estado Actual - Sistema OpsBase (13/03/2026)

## 1. Proposito del documento
- Definir la base funcional y tecnica de `OpsBase`.
- Convertir la vision del proyecto en requisitos ejecutables.
- Servir como contrato inicial para arquitectura, desarrollo y validacion.

## 2. Vision del sistema
`OpsBase` es una plataforma nodriza metadata-driven para generar sistemas operativos trazables.

No es un sistema de inventario cerrado a un rubro. Es un motor universal que permite modelar y operar:
- recursos/activos
- movimientos
- estados
- workflows
- reglas
- documentos
- permisos
- vistas
- auditoria
- integraciones

Debe adaptarse por configuracion a distintos dominios, por ejemplo:
- Policia
- Hospital
- Depositos judiciales
- Logistica
- Ecommerce
- Industria
- Farmacia
- Taller
- Activos IT
- Abastecimiento
- Mantenimiento

## 3. Objetivo general
Disenar y construir una arquitectura capaz de generar sistemas operativos completos desde un nucleo comun, con foco en:
- inventario avanzado
- logistica integral
- trazabilidad total
- control operativo
- documentacion
- reglas y workflows
- analitica operacional

## 4. Principios arquitectonicos
1. Universalidad por diseno
- Nada debe estar acoplado al dominio "producto tradicional".

2. Metadata primero
- Estructura, comportamiento y experiencia se definen por metadata cuando sea razonable.

3. Nucleo estable + extension por modulos
- El core se mantiene pequeno y robusto.
- Las capacidades de dominio se agregan por modulos y configuracion.

4. Trazabilidad end-to-end
- Cada operacion debe ser auditable por actor, fecha, contexto y resultado.

5. Separacion de responsabilidades
- Core
- Metadata
- Runtime
- Generador
- Seguridad
- Auditoria
- Integraciones

## 5. Capas de la solucion
1. Nucleo universal
- Contratos base, tipos comunes, seguridad, auditoria, eventos, tenancy.

2. Capa de metadata
- Definiciones de recursos, atributos, operaciones, estados, vistas, reglas, workflows.

3. Motor generador/runtime
- Interpreta metadata y expone APIs, formularios, listas, reglas y flujos ejecutables.

4. Instancias operativas por dominio
- Configuraciones por organizacion/caso de uso sobre el mismo motor.

## 6. Alcance funcional objetivo (completo)
OpsBase debe cubrir estos universos funcionales:

1. Administracion base
- empresas, sucursales, usuarios, roles, permisos, autenticacion, auditoria.

2. Catalogo universal
- tipos de recurso, atributos dinamicos, categorias, variantes, unidades, relaciones.

3. Inventario avanzado
- stock real/reservado/disponible/comprometido/transito/danado/vencido/bloqueado/cuarentena/reparacion.

4. Motor de movimientos
- ingreso, egreso, transferencia, ajuste, reserva, liberacion, picking, packing, despacho, recepcion, devolucion, merma, rotura, perdida, reparacion, baja, reubicacion.

5. Deposits y ubicaciones
- nodos, depositos, sectores, pasillos, estanterias, niveles, posiciones, restricciones, capacidad.

6. Lectura y captura operativa
- barcode, QR, DataMatrix, RFID, escaneo movil/pistola, etiquetas, impresion.

7. Compras y abastecimiento
- solicitudes, requisiciones, ordenes, proveedores, recepcion, calidad, devolucion a proveedor.

8. Salidas y consumo
- ventas, entregas, asignaciones internas, consumo operativo, prestamos, bajas, salida a reparacion.

9. Logistica y envios
- picking/packing/despacho, tracking, transportistas, rutas, entregas, devoluciones, POD, geolocalizacion.

10. Trazabilidad total
- por item, lote, serie, ubicacion, documento, usuario, operacion, envio.

11. Mantenimiento y activos
- preventivo/correctivo, historial tecnico, garantias, repuestos, vida util.

12. Gestion documental
- actas, remitos, ordenes, recepciones, comprobantes, adjuntos, fotos, certificados, firmas.

13. Motor de reglas
- validaciones, bloqueos, automatizaciones, alertas, condiciones por estado/tipo/ubicacion/documento.

14. Workflows y aprobaciones
- estados, transiciones, aprobaciones simples y jerarquicas, rechazo, observaciones, firmas.

15. Reportes y analitica
- stock, movimientos, vencimientos, diferencias, productividad, ocupacion, costos, KPIs, dashboards.

16. Integraciones
- APIs externas, lectores, balanzas, impresoras, ERP, facturacion, correo, mensajeria, mapas, OCR, IA.

## 7. Requisitos del motor generador
La plataforma debe permitir configurar sin tocar codigo:
- tipos de recurso
- atributos dinamicos
- tipos de documento
- tipos de movimiento
- estados y transiciones
- reglas
- formularios
- vistas
- dashboards
- workflows
- permisos
- modulos activos
- relaciones entre entidades
- automatizaciones
- alertas
- integraciones

Requisito clave:
- El motor debe generar comportamiento operativo, no solo tablas CRUD.

## 8. Modelo conceptual base (abstracciones)
Entidades conceptuales requeridas:
- `ResourceDefinition`: define el tipo de recurso.
- `ResourceInstance`: instancia operativa del recurso.
- `AttributeDefinition`: esquema de atributo dinamico.
- `AttributeValue`: valor dinamico por instancia.
- `OperationDefinition`: define acciones operativas.
- `OperationExecution`: ejecucion real de una operacion.
- `WorkflowDefinition`: estados y transiciones.
- `WorkflowInstance`: estado actual y historial del flujo.
- `DocumentDefinition`: plantilla/tipo documental.
- `DocumentInstance`: documento emitido/adjunto.
- `RuleDefinition`: condicion + accion.
- `RuleExecution`: evaluacion y resultado.
- `ViewDefinition`: composicion de UI runtime.
- `PermissionDefinition`: permisos por accion/contexto.
- `IntegrationDefinition`: conectores y contratos externos.

## 9. Stack tecnologico objetivo
Backend:
- .NET 8
- C#
- EF Core
- SQL Server
- JWT
- Arquitectura por Controllers + Gestores/Services + Entidades + DTOs + Routes

Frontend:
- Vue 3
- Vuetify
- JavaScript/TypeScript
- Componentes reutilizables
- Vistas dinamicas desde metadata

Extras:
- Docker
- SignalR
- Escaneo por camara
- Import/export
- PDF
- Dashboards interactivos

## 10. Requisitos no funcionales iniciales
Seguridad:
- JWT + control de permisos por operacion/recurso.
- Auditoria obligatoria de acciones criticas.

Escalabilidad:
- Preparado para multiempresa, multisucursal, multideposito y multirol.
- Evolucion a multi-tenant SaaS.

Observabilidad:
- Logs estructurados por operacion.
- Trazas con correlacion de solicitudes.

Performance (objetivos iniciales):
- Consultas de listados paginados y filtrables.
- Operaciones de movimiento en transaccion atomica.

Confiabilidad:
- Reintentos controlados en integraciones.
- Idempotencia para operaciones sensibles.

## 11. MVP Fase 1 (alcance cerrado)
Para iniciar sin sobrediseno, Fase 1 incluye solo 4 modulos:

1. Administracion base
- Empresa, sucursal, usuario, rol, permiso.

2. Catalogo universal de recursos
- `ResourceDefinition`, `AttributeDefinition`, categorias y unidades.

3. Inventario + ubicaciones + movimientos base
- Stock por ubicacion.
- Movimientos: ingreso, egreso, transferencia, ajuste, reserva/liberacion.

4. Trazabilidad y auditoria operacional
- Historial de operaciones por recurso, usuario y documento.

Fuera de Fase 1:
- Compras avanzadas, logistica avanzada, mantenimiento completo, OCR/IA avanzada, dashboards complejos.

## 12. Criterios de aceptacion del MVP
1. Se puede crear un tipo de recurso por metadata sin codigo.
2. El runtime genera formulario y grilla para ese tipo.
3. Se registra un recurso y se ejecutan movimientos base.
4. El stock se actualiza correctamente por ubicacion.
5. Cada movimiento queda auditado y trazable.
6. El mismo motor se valida en 3 verticales:
- Policia
- Hospital
- Logistica

## 13. Matriz: metadata vs logica fija vs hibrido
Por metadata:
- Definicion de recursos y atributos
- Formularios y vistas runtime
- Reglas declarativas simples
- Workflows declarativos
- Tipos documentales

Logica fija en core:
- Autenticacion/autorizacion
- Motor transaccional de stock
- Auditoria y trazabilidad
- Integridad referencial critica
- Contratos base de integracion

Hibrido:
- Reglas complejas (parte declarativa + evaluador en core)
- Workflows con pasos manuales + acciones automaticas
- Integraciones configurables sobre adaptadores base

## 14. Verticales de validacion (obligatorias)
Policia:
- Recurso: evidencia
- Flujo: ingreso -> cadena de custodia -> transferencia -> baja judicial

Hospital:
- Recurso: medicamento/insumo
- Flujo: ingreso -> almacenamiento -> consumo por area -> trazabilidad por lote/vencimiento

Logistica:
- Recurso: paquete/pallet
- Flujo: recepcion -> ubicacion -> picking -> despacho -> entrega/devolucion

## 15. Backlog inicial (epicas)
EP1. Core y seguridad
- Tenancy organizacional, usuarios, roles, permisos, JWT, auditoria base.

EP2. Metadata base
- Definicion de recursos, atributos, validaciones, vistas basicas.

EP3. Runtime dinamico
- CRUD dinamico de recursos + filtros + formularios por metadata.

EP4. Motor de stock y movimientos
- Kardex base, stock por ubicacion, movimientos base, reglas de consistencia.

EP5. Trazabilidad
- Timeline por recurso y consulta de historial operativo.

EP6. Validacion vertical
- Caso Policia, Hospital y Logistica sobre el mismo motor.

## 16. Riesgos arquitectonicos y mitigacion
Riesgo: Sobrediseno por universalidad extrema.
- Mitigacion: Fase 1 cerrada, incremental, con criterios medibles.

Riesgo: Metadata demasiado libre y sin gobernanza.
- Mitigacion: versionado de metadata, validadores y contratos de compatibilidad.

Riesgo: Complejidad alta en reglas/workflows.
- Mitigacion: iniciar con DSL simple + evaluador controlado.

Riesgo: Caida de performance por dinamismo.
- Mitigacion: indices por contexto, cache selectiva y consultas paginadas.

## 17. Decisiones abiertas (para cerrar antes de construir)
1. Estrategia de multi-tenant:
- database por tenant, schema por tenant o tenant_id compartido.

2. Nivel de dinamismo:
- SQL dinamico con tablas por recurso vs modelo mixto con columnas fijas + atributos.

3. Lenguaje de reglas:
- JSON declarativo, expresiones, o motor dedicado.

4. Alcance de documentos en Fase 1:
- solo adjuntos basicos vs plantillas + generacion documental.

## 18. Proximos pasos inmediatos
1. Congelar MVP Fase 1 (alcance y exclusiones).
2. Definir modelo de datos inicial (tablas core + metadata + operaciones).
3. Disenar contratos API base del runtime dinamico.
4. Implementar primer flujo end-to-end en vertical Logistica.
5. Repetir validacion en Policia y Hospital sin cambios de core.

## 19. Historial de cambios
- 13/03/2026: Documento base creado.
- 13/03/2026: Requisitos consolidados y normalizados para iniciar desarrollo.
- 17/03/2026: Seguridad por permisos aplicada en API/runtime (`ops.*`) con bootstrap SQL en `docs/sql/opsbase-security-bootstrap.sql`.
- 17/03/2026: Validacion vertical ampliada con scripts smoke de `logistica`, `policia` y `hospital`.
- 17/03/2026: Flujo operativo real iniciado con `Recepcion guiada` (wizard frontend + endpoint transaccional `POST /api/v1/ops-flow/recepcion`).
