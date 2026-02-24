import os
import yaml
from dotenv import load_dotenv

from .overpass_client import OverpassClient
from .build_dataset import build_all

def load_config():
    with open("config/settings.yaml", "r", encoding="utf-8") as f:
        return yaml.safe_load(f)

def main():
    load_dotenv()  # lee .env si existe

    cfg = load_config()
    area_name = os.getenv("AREA_NAME", cfg["area_name"])
    area_scope = os.getenv("AREA_SCOPE", cfg.get("area_scope", "city"))
    overpass_url = os.getenv("OVERPASS_URL", cfg["overpass_url"])
    out_dir = os.getenv("OUT_DIR", cfg["out_dir"])

    client = OverpassClient(overpass_url, timeout_sec=cfg["timeout_sec"], retries=cfg["retries"])
    build_all(client, area_name, out_dir, area_scope=area_scope)

    print("Listo ->", out_dir)

if __name__ == "__main__":
    main()
