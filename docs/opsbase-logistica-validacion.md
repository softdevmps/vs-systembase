# OpsBase - Validacion Vertical Logistica (Smoke)

Este smoke valida un flujo operativo completo sobre el mismo core de OpsBase:

1. Ingreso (borrador -> confirmado)
2. Transferencia (borrador -> confirmado)
3. Egreso (borrador -> confirmado)
4. Reserva (borrador -> confirmado)
5. Liberacion (borrador -> confirmado)

Incluye verificaciones automaticas de:

- Stock final por ubicacion (`StockReal`, `StockReservado`, `StockDisponible`)
- Trazabilidad/auditoria por `ResourceInstance` (timeline)

## Requisitos

- Backend de OpsBase levantado (por defecto en `http://127.0.0.1:6036`)
- Usuario admin habilitado (`admin/admin` por defecto)
- `curl` y `jq`
- `StockBalance` con indice unico compuesto por (`ResourceInstanceId`, `LocationId`)
- Seguridad bootstrap aplicada (opcional, recomendado):
  - `docs/sql/opsbase-security-bootstrap.sql`

## Ejecucion

```bash
./docs/scripts/opsbase-logistica-smoke.sh
```

Tambien estan disponibles los smokes de las otras verticales de Fase 1:

```bash
./docs/scripts/opsbase-policia-smoke.sh
./docs/scripts/opsbase-hospital-smoke.sh
```

Variables opcionales:

```bash
API_BASE=http://127.0.0.1:6036/api/v1 \
OPS_USER=admin \
OPS_PASS=admin \
RUN_TAG=manual001 \
./docs/scripts/opsbase-logistica-smoke.sh
```

## Resultado esperado

Si todo esta correcto, cada script imprime `SMOKE ... OK` y un resumen con:

- IDs creados
- Stock final en origen/destino
- Cantidad de eventos de timeline

## Troubleshooting rapido

Si falla en una transferencia con error de `StockBalance`/indice unico:

```sql
:r docs/sql/opsbase-fix-stockbalance-index.sql
```

Luego re-ejecutar el smoke.

Si aun no habilitaste seguridad/roles:

```sql
:r docs/sql/opsbase-security-bootstrap.sql
```
