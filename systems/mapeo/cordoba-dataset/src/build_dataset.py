# src/build_dataset.py
import os
import re
import pandas as pd
from typing import Optional

from .normalize import normalize_name, infer_tipo_via
from .overpass_client import OverpassClient, area_query


def _build_area_prefix(area_name: str, area_scope: str) -> str:
    scope = (area_scope or "city").strip().lower()
    admin_level = "8" if scope == "city" else "4"
    return area_query(area_name or "Córdoba", admin_level)


def _parse_altura(value) -> Optional[int]:
    if value is None:
        return None
    m = re.search(r"\d{1,5}", str(value))
    if not m:
        return None
    altura = int(m.group(0))
    if altura < 1 or altura > 20000:
        return None
    return altura


def _is_valid_street_name(name: str) -> bool:
    if not name:
        return False
    if len(name.strip()) < 3:
        return False
    if re.fullmatch(r"[\d\-\s/]+", name):
        return False
    return bool(re.search(r"[A-ZÑÁÉÍÓÚ]", name))


def fetch_vias(client: OverpassClient, area_name: str, area_scope: str = "city") -> pd.DataFrame:
    """
    Trae vías desde OSM (ways) que tengan highway + name.
    Devuelve un DF con nombre original, normalizado, tipo_via e info básica.
    """
    q = _build_area_prefix(area_name, area_scope) + """
(
  way(area.a)["highway"]["name"];
);
out tags;
"""
    data = client.query(q)

    rows = []
    for el in data.get("elements", []):
        tags = el.get("tags", {}) or {}
        name = tags.get("name", "")
        if not name:
            continue

        nname = normalize_name(name)
        if not _is_valid_street_name(nname):
            continue
        rows.append(
            {
                "osm_id": f"way/{el.get('id')}",
                "nombre_original": name,
                "nombre_normalizado": nname,
                "tipo_via": infer_tipo_via(nname),
                "highway": tags.get("highway", ""),
            }
        )

    df = pd.DataFrame(rows)
    if df.empty:
        return df

    df = df.drop_duplicates(subset=["nombre_normalizado"])
    df = df.sort_values(["tipo_via", "nombre_normalizado"]).reset_index(drop=True)
    return df


def fetch_direcciones(client: OverpassClient, area_name: str, area_scope: str = "city") -> pd.DataFrame:
    """
    Trae direcciones desde OSM (nodes/ways) con:
      addr:street + addr:housenumber
    Devuelve DF con calle + altura + lat/lon.
    """
    q = _build_area_prefix(area_name, area_scope) + """
(
  node(area.a)["addr:housenumber"]["addr:street"];
  way(area.a)["addr:housenumber"]["addr:street"];
);
out center tags;
"""
    data = client.query(q)

    rows = []
    for el in data.get("elements", []):
        tags = el.get("tags", {}) or {}
        street = tags.get("addr:street", "")
        num = tags.get("addr:housenumber", "")
        if not street or not num:
            continue

        altura = _parse_altura(num)
        street_norm = normalize_name(street)
        if altura is None or not _is_valid_street_name(street_norm):
            continue

        # Coordenadas:
        # - node: lat/lon directos
        # - way: viene en el centro (center.lat/center.lon)
        lat = el.get("lat")
        lon = el.get("lon")
        if lat is None or lon is None:
            center = el.get("center") or {}
            lat = center.get("lat")
            lon = center.get("lon")

        # Si sigue sin coords, saltamos
        if lat is None or lon is None:
            continue

        rows.append(
            {
                "osm_id": f"{el.get('type')}/{el.get('id')}",
                "calle_original": street,
                "calle_normalizada": street_norm,
                "altura": altura,
                "lat": float(lat),
                "lon": float(lon),
            }
        )

    df = pd.DataFrame(rows)
    if df.empty:
        return df

    df = df.drop_duplicates(subset=["calle_normalizada", "altura", "lat", "lon"])
    df = df.sort_values(["calle_normalizada", "altura"]).reset_index(drop=True)
    return df


def build_all(client: OverpassClient, area_name: str, out_dir: str, area_scope: str = "city") -> None:
    """
    Construye y guarda:
      - vias.csv
      - direcciones.csv
      - direcciones_enriquecidas.csv (direcciones + tipo_via)
    """
    os.makedirs(out_dir, exist_ok=True)

    # 1) Vías
    vias = fetch_vias(client, area_name, area_scope=area_scope)
    vias_path = os.path.join(out_dir, "vias.csv")
    vias.to_csv(vias_path, index=False, encoding="utf-8")

    # 2) Direcciones
    direc = fetch_direcciones(client, area_name, area_scope=area_scope)
    direc_path = os.path.join(out_dir, "direcciones.csv")
    direc.to_csv(direc_path, index=False, encoding="utf-8")

    # 3) Enriquecimiento (tipo_via)
    if direc.empty:
        enriched = direc.copy()
        enriched["tipo_via"] = []
    else:
        if vias.empty:
            enriched = direc.copy()
            enriched["tipo_via"] = enriched["calle_normalizada"].apply(infer_tipo_via)
        else:
            vias_key = vias[["nombre_normalizado", "tipo_via"]].rename(
                columns={"nombre_normalizado": "calle_normalizada"}
            )
            enriched = direc.merge(vias_key, on="calle_normalizada", how="left")
            enriched["tipo_via"] = enriched["tipo_via"].fillna(
                enriched["calle_normalizada"].apply(infer_tipo_via)
            )

    enriched_path = os.path.join(out_dir, "direcciones_enriquecidas.csv")
    enriched.to_csv(enriched_path, index=False, encoding="utf-8")
