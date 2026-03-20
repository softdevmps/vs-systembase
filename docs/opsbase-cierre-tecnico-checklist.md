# OpsBase - Cierre Tecnico (QA + Hardening)

## 1) Flujo E2E operativo

1. Carga inicial:
- Crear tipo de recurso.
- Crear instancia.
- Crear deposito.
- Crear stock inicial.

2. Operacion:
- Recepcionar en borrador.
- Confirmar desde historial rapido.
- Despachar o trasladar.
- Confirmar/anular/reintentar desde historial rapido.

3. Control:
- Revisar Pendientes.
- Validar Kardex.
- Revisar Trazabilidad por recurso y por deposito.

## 2) Integridad de datos (DB)

Verificar:
- Unicidad de `StockBalance` por (`ResourceInstanceId`, `LocationId`).
- PK/FK activas para `Movement`, `MovementLine`, `StockBalance`, `Location`.
- Sin valores negativos invalidos:
  - `StockReal >= 0`
  - `StockReservado >= 0`
  - `StockReservado <= StockReal`

## 3) Permisos por rol

Validar con usuario operador y admin:
- `ops.movement.view`
- `ops.movement.create`
- `ops.movement.update`
- `ops.movement.confirm`
- `ops.movement.delete`
- `ops.movementline.*`
- `ops.location.*`
- `ops.stockbalance.*`

## 4) Dataset real (sin sinteticos)

Minimo recomendado:
- 3 rubros.
- 10 tipos de recurso.
- 100+ instancias.
- 15+ depositos con coordenadas reales.
- 200+ movimientos distribuidos en ingreso/egreso/transferencia.

## 5) Criterios de salida

Se considera cerrado cuando:
- El flujo end-to-end funciona sin ir al ABM tecnico.
- No hay errores 5xx en consola/backend en operaciones de negocio.
- Los estados de movimiento y stock son consistentes post-operacion.
- Trazabilidad permite abrir movimiento origen desde timeline.
