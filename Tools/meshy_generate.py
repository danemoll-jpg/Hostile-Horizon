"""Fix 3.1 stage 3: concept images -> textured 3D models with the Meshy image-to-3D API.

python Tools/meshy_generate.py [--only pilot,mimic] [--force]

Reads SourceArt/Concepts/<name>.png, writes SourceArt/Meshy/<name>/ (model.glb, model.fbx, textures, preview.png,
task.json). The API key comes from MESHY_API_KEY (process environment, else the Windows user environment); it is never
printed, logged or written to a file. A finished model is skipped unless --force; an unfinished task is resumed.
"""
import argparse, base64, json, os, sys, time, urllib.request, urllib.error
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API = "https://api.meshy.ai/openapi/v1/image-to-3d"
# triangle budgets (TODO.md Fix 3.1): pilot 15-20k, creatures 20-30k
MODELS = {"pilot": 18000, "mimic": 25000, "hollow": 25000, "knocker": 28000, "stalker": 25000, "sleeper": 25000}


def key():
    k = os.environ.get("MESHY_API_KEY")
    if not k and sys.platform == "win32":
        import winreg
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as r:
                k = winreg.QueryValueEx(r, "MESHY_API_KEY")[0]
        except OSError:
            k = None
    if not k:
        sys.exit("MESHY_API_KEY is not set")
    return k


def call(method, url, k, body=None):
    req = urllib.request.Request(url, method=method, headers={"Authorization": "Bearer " + k, "Content-Type": "application/json"},
                                 data=json.dumps(body).encode() if body is not None else None)
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return json.loads(r.read())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"{method} {url.split('?')[0]}: HTTP {e.code} {e.read()[:400].decode(errors='replace')}") from None


def download(url, path):
    with urllib.request.urlopen(url, timeout=300) as r, open(path, "wb") as f:
        f.write(r.read())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    a = ap.parse_args()
    names = [n for n in MODELS if not a.only or n in a.only.split(",")]
    k = key()
    tasks = {}
    for n in names:
        out = ROOT / "SourceArt" / "Meshy" / n
        out.mkdir(parents=True, exist_ok=True)
        tj = out / "task.json"
        if tj.exists() and not a.force:
            t = json.loads(tj.read_text())
            if t.get("status") == "SUCCEEDED" and (out / "model.glb").exists():
                print(f"{n}: already done (task {t['id']})"); continue
            if t.get("status") not in ("FAILED", "CANCELED"):
                tasks[n] = t["id"]; print(f"{n}: resuming task {t['id']}"); continue
        img = ROOT / "SourceArt" / "Concepts" / f"{n}.png"
        body = {"image_url": "data:image/png;base64," + base64.b64encode(img.read_bytes()).decode(),
                "ai_model": "latest", "should_remesh": True, "topology": "triangle", "target_polycount": MODELS[n],
                "should_texture": True, "enable_pbr": True, "pose_mode": "a-pose", "remove_lighting": True}
        tid = call("POST", API, k, body)["result"]
        tj.write_text(json.dumps({"id": tid, "status": "PENDING", "polycount": MODELS[n]}, indent=1))
        tasks[n] = tid
        print(f"{n}: started task {tid}")
    while tasks:
        time.sleep(15)
        for n, tid in list(tasks.items()):
            t = call("GET", f"{API}/{tid}", k)
            out = ROOT / "SourceArt" / "Meshy" / n
            if t["status"] in ("PENDING", "IN_PROGRESS"):
                print(f"{n}: {t['status']} {t.get('progress', 0)}%", flush=True); continue
            del tasks[n]
            keep = {x: t.get(x) for x in ("id", "status", "consumed_credits", "task_error", "created_at", "finished_at")}
            keep["polycount"] = MODELS[n]
            if t["status"] != "SUCCEEDED":
                (out / "task.json").write_text(json.dumps(keep, indent=1)); print(f"{n}: {t['status']} {t.get('task_error')}"); continue
            for fmt in ("glb", "fbx"):
                if t["model_urls"].get(fmt):
                    download(t["model_urls"][fmt], out / f"model.{fmt}")
            for i, tex in enumerate(t.get("texture_urls") or []):
                for ch, url in tex.items():
                    if url: download(url, out / (f"{ch}.png" if i == 0 else f"{ch}_{i}.png"))
            if t.get("thumbnail_url"): download(t["thumbnail_url"], out / "preview.png")
            (out / "task.json").write_text(json.dumps(keep, indent=1))
            print(f"{n}: SUCCEEDED, {t.get('consumed_credits')} credits, saved to {out.relative_to(ROOT)}", flush=True)


if __name__ == "__main__":
    main()
