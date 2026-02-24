import re
import unicodedata

def strip_accents(s: str) -> str:
    return "".join(
        c for c in unicodedata.normalize("NFD", s)
        if unicodedata.category(c) != "Mn"
    )

def normalize_name(s: str) -> str:
    if not s:
        return ""
    s = strip_accents(s.strip()).upper()
    s = re.sub(r"[\.\,;:\(\)\[\]\{\}]", " ", s)
    s = re.sub(r"\s+", " ", s).strip()

    repl = {
        r"\bAV\b": "AVENIDA",
        r"\bAVDA\b": "AVENIDA",
        r"\bBV\b": "BOULEVARD",
        r"\bPJE\b": "PASAJE",
        r"\bPSJE\b": "PASAJE",
        r"\bCNO\b": "CAMINO",
        r"\bRN\b": "RUTA NACIONAL",
        r"\bRP\b": "RUTA PROVINCIAL",
    }
    for pat, rep in repl.items():
        s = re.sub(pat, rep, s)

    return re.sub(r"\s+", " ", s).strip()

def infer_tipo_via(nombre_norm: str) -> str:
    if not nombre_norm:
        return "OTRO"
    for pref, tipo in [
        ("AVENIDA ", "AVENIDA"),
        ("PASAJE ", "PASAJE"),
        ("BOULEVARD ", "BOULEVARD"),
        ("CAMINO ", "CAMINO"),
        ("RUTA NACIONAL ", "RUTA"),
        ("RUTA PROVINCIAL ", "RUTA"),
        ("RUTA ", "RUTA"),
        ("DIAGONAL ", "DIAGONAL"),
        ("CALLE ", "CALLE"),
    ]:
        if nombre_norm.startswith(pref):
            return tipo
    return "CALLE"