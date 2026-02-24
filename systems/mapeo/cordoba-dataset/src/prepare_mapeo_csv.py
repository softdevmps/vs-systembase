import argparse
from pathlib import Path

import pandas as pd


def _normalize_altura(series: pd.Series) -> pd.Series:
    text = series.astype(str).str.extract(r"(?P<n>\d{1,5})")["n"]
    return pd.to_numeric(text, errors="coerce")


def build_mapeo_csv(
    input_path: Path,
    output_path: Path,
    lat_min: float = -31.55,
    lat_max: float = -31.30,
    lon_min: float = -64.30,
    lon_max: float = -64.05,
) -> None:
    if not input_path.exists():
        raise SystemExit(f"No existe input: {input_path}")

    df = pd.read_csv(input_path)
    required = {"calle_normalizada", "calle_original", "altura", "lat", "lon"}
    missing = required.difference(df.columns)
    if missing:
        raise SystemExit(f"Faltan columnas en CSV: {sorted(missing)}")

    if "tipo_via" not in df.columns:
        df["tipo_via"] = "CALLE"

    # Normalizacion de tipos
    df["calle_normalizada"] = df["calle_normalizada"].astype(str).str.strip().str.upper()
    df["calle_original"] = df["calle_original"].astype(str).str.strip()
    df["altura"] = _normalize_altura(df["altura"])
    df["lat"] = pd.to_numeric(df["lat"], errors="coerce")
    df["lon"] = pd.to_numeric(df["lon"], errors="coerce")

    # Filtro geográfico aproximado Córdoba capital
    df = df[
        df["lat"].between(lat_min, lat_max)
        & df["lon"].between(lon_min, lon_max)
    ]

    # Filtro de validez
    df = df[
        df["calle_normalizada"].str.len().ge(3)
        & df["altura"].notna()
        & df["lat"].notna()
        & df["lon"].notna()
    ]
    df["altura"] = df["altura"].astype(int)

    # Consolidar por calle+altura (mediana de puntos)
    grouped = (
        df.groupby(["calle_normalizada", "altura"], as_index=False)
        .agg(
            calle_original=("calle_original", "first"),
            lat=("lat", "median"),
            lon=("lon", "median"),
            tipo_via=("tipo_via", "first"),
        )
        .sort_values(["calle_normalizada", "altura"])
        .reset_index(drop=True)
    )

    grouped = grouped.rename(columns={"lon": "lng"})

    output_path.parent.mkdir(parents=True, exist_ok=True)
    grouped.to_csv(output_path, index=False, encoding="utf-8")

    print(f"OK -> {output_path}")
    print(f"Rows: {len(grouped)}")
    print(
        f"BBox: lat[{lat_min}, {lat_max}] lon[{lon_min}, {lon_max}]"
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", default="out/direcciones_enriquecidas.csv")
    parser.add_argument("--output", default="out/direcciones_mapeo.csv")
    parser.add_argument("--lat-min", type=float, default=-31.55)
    parser.add_argument("--lat-max", type=float, default=-31.30)
    parser.add_argument("--lon-min", type=float, default=-64.30)
    parser.add_argument("--lon-max", type=float, default=-64.05)
    args = parser.parse_args()

    base = Path(__file__).resolve().parent.parent
    input_path = Path(args.input)
    output_path = Path(args.output)
    if not input_path.is_absolute():
        input_path = base / input_path
    if not output_path.is_absolute():
        output_path = base / output_path

    build_mapeo_csv(
        input_path=input_path,
        output_path=output_path,
        lat_min=args.lat_min,
        lat_max=args.lat_max,
        lon_min=args.lon_min,
        lon_max=args.lon_max,
    )


if __name__ == "__main__":
    main()
