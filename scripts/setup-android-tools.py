"""Install local Android/JDK build tools from official HTTPS artifacts with checksums.
No application files, system trust settings or production signing keys are changed.
"""
import hashlib
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

workspace = Path("/workspace")
jdk_version = "17.0.20.1+1"
jdk_name = "OpenJDK17U-jdk_x64_linux_hotspot_17.0.20.1_1.tar.gz"
jdk_url = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.20.1%2B1/" + jdk_name
jdk_root = workspace / "fwf-jdk"
if not (jdk_root / ("jdk-" + jdk_version) / "bin/jar").exists():
    with tempfile.TemporaryDirectory(prefix="fwf-jdk-") as temporary:
        archive = Path(temporary) / jdk_name
        # curl uses the provided proxy/trust configuration and never disables TLS checks.
        subprocess.run(["curl", "-fsSL", jdk_url, "-o", str(archive)], check=True)
        checksum = Path(temporary) / "checksum"
        subprocess.run(["curl", "-fsSL", jdk_url + ".sha256.txt", "-o", str(checksum)], check=True)
        if hashlib.file_digest(archive.open("rb"), "sha256").hexdigest() != checksum.read_text().split()[0]:
            raise SystemExit("JDK checksum mismatch")
        jdk_root.mkdir(exist_ok=True)
        with tarfile.open(archive) as package:
            package.extractall(jdk_root, filter="data")

sdk_root = workspace / "fwf-android-sdk"
sdk_root.mkdir(exist_ok=True)
with tempfile.TemporaryDirectory(prefix="fwf-android-") as temporary:
    repository = Path(temporary) / "repository.xml"
    subprocess.run(["curl", "-fsSL", "https://dl.google.com/android/repository/repository2-1.xml", "-o", str(repository)], check=True)
    root = ET.parse(repository).getroot()
    for package_path, target in [("platforms;android-34", "platforms/android-34"),
                                 ("build-tools;34.0.0", "build-tools/34.0.0"),
                                 ("platform-tools", "platform-tools")]:
        destination = sdk_root / target
        if (destination / "source.properties").exists():
            continue
        package = next(p for p in root.findall("remotePackage") if p.get("path") == package_path)
        artifact = next(a for a in package.findall("./archives/archive") if a.findtext("host-os") in (None, "linux"))
        filename = artifact.findtext("./complete/url")
        expected = artifact.findtext("./complete/checksum")
        archive = Path(temporary) / filename
        subprocess.run(["curl", "-fsSL", "https://dl.google.com/android/repository/" + filename, "-o", str(archive)], check=True)
        # Use the official repository's published checksum; never change a mismatch to match local bytes.
        if hashlib.file_digest(archive.open("rb"), "sha1").hexdigest() != expected:
            raise SystemExit("Android SDK checksum mismatch")
        staging = Path(temporary) / "staging"
        if staging.exists(): shutil.rmtree(staging)
        staging.mkdir()
        with zipfile.ZipFile(archive) as content:
            for entry in content.infolist():
                output = (staging / entry.filename).resolve()
                if not output.is_relative_to(staging.resolve()): raise SystemExit("Unsafe artifact path")
            content.extractall(staging)
            for entry in content.infolist():
                mode = entry.external_attr >> 16
                if mode: (staging / entry.filename).chmod(mode & 0o777)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copytree(next(staging.iterdir()), destination, dirs_exist_ok=True)
print("Android/JDK tools prepared:", sdk_root, jdk_root / ("jdk-" + jdk_version))
