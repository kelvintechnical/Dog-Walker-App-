"""Some Linux workload installers skip package verification; verify before building."""
import os
from pathlib import Path
import shutil
import subprocess
sdk_root = Path(os.environ.get("DOTNET_ROOT", Path(shutil.which("dotnet")).resolve().parent))
packages = [p for folder in ("packs", "library-packs", "template-packs")
            for p in (sdk_root / folder).rglob("*.nupkg")]
if not packages:
    raise SystemExit("No workload packages available for verification")
for package in packages:
    subprocess.run(["dotnet", "nuget", "verify", str(package), "--all"], check=True)
print("Verified workload packages:", len(packages))
