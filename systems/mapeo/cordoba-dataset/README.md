# Cordoba Dataset (OSM)

Genera CSV de calles y direcciones para usar como dataset local del sistema de mapeo.

## Requisitos

```bash
cd systems/mapeo/cordoba-dataset
python3 -m pip install -r requirements.txt
```

## Variables

- `AREA_SCOPE`: `city` (recomendado) o `province`
- `AREA_NAME`: por defecto `Córdoba`
- `OVERPASS_URL`: endpoint Overpass
- `OUT_DIR`: carpeta de salida

Podés copiarlas desde `.env.example` a `.env`.

## Generación

```bash
cd systems/mapeo/cordoba-dataset
python3 -m src.main
```

Salida en `out/`:

- `vias.csv`
- `direcciones.csv`
- `direcciones_enriquecidas.csv`

## Preparar CSV para mapeo (recomendado)

Genera un CSV limpio para importar al backend (`Córdoba capital`, sin ruido).

```bash
cd systems/mapeo/cordoba-dataset
python3 -m src.prepare_mapeo_csv --input out/direcciones_enriquecidas.csv --output out/direcciones_mapeo.csv
```

Salida:

- `out/direcciones_mapeo.csv` con columnas:
  - `calle_normalizada`
  - `altura`
  - `calle_original`
  - `lat`
  - `lng`
  - `tipo_via`

## Calidad de datos

El pipeline ya filtra:

- nombres de calle inválidos (solo números/símbolos)
- alturas fuera de rango (`1..20000`)
- duplicados por calle+altura+coordenada

Además, usa `AREA_SCOPE=city` por defecto para evitar ruido de toda la provincia.

## Próximo paso recomendado

Importar `out/direcciones_enriquecidas.csv` a SQL (`sys_mapeo`) y usarlo como primer geocoder (antes de Nominatim).
