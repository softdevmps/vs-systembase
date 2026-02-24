# src/overpass_client.py
import time
import requests
from typing import Dict, Any


class OverpassClient:
    def __init__(self, base_url: str, timeout_sec: int = 180, retries: int = 3):
        self.base_url = base_url
        self.timeout_sec = timeout_sec
        self.retries = retries

    def query(self, q: str) -> Dict[str, Any]:
        last_err = None
        for i in range(self.retries):
            try:
                r = requests.post(self.base_url, data={"data": q}, timeout=self.timeout_sec)
                r.raise_for_status()
                return r.json()
            except Exception as e:
                last_err = e
                time.sleep(2 * (i + 1))
        raise RuntimeError(f"Overpass falló: {last_err}")


def area_query(area_name: str, admin_level: str = "8") -> str:
    safe_name = (area_name or "").replace('"', '\\"')
    safe_level = str(admin_level or "8")
    return f"""
[out:json][timeout:180];
rel["boundary"="administrative"]["admin_level"="{safe_level}"]["name"="{safe_name}"];
map_to_area->.a;
"""


def area_cordoba_ciudad() -> str:
    return area_query("Córdoba", "8")


def area_provincia_cordoba() -> str:
    return """
[out:json][timeout:180];
rel["boundary"="administrative"]["admin_level"="4"]["name"="Córdoba"]["ISO3166-2"="AR-X"];
map_to_area->.a;
"""
