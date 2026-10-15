#!/usr/bin/env python3
"""Linux structure/identifier validator for the Unity project skeleton.

Runs without a Unity editor. Checks required files, bundle-ID consistency between
ProjectSettings and toolchain.json, assembly definitions, package manifest and
that generated Unity folders are not committed. Exit 1 on any failure.
"""
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
errors = []


def fail(msg):
    errors.append(msg)


def read(rel):
    p = ROOT / rel
    if not p.is_file():
        fail(f"missing required file: {rel}")
        return ""
    return p.read_text(encoding="utf-8")


REQUIRED = [
    "ProjectSettings/ProjectVersion.txt",
    "ProjectSettings/ProjectSettings.asset",
    "ProjectSettings/EditorBuildSettings.asset",
    "ProjectSettings/GraphicsSettings.asset",
    "Packages/manifest.json",
    "Assets/Scenes/Nova.unity",
    "Assets/Settings/URP-Performant.asset",
    "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset",
    ".gitattributes",
    ".gitignore",
    ".github/workflows/ci.yml",
    "AppStore/description.txt",
    "AppStore/icon.png",
    "toolchain.json",
]
for rel in REQUIRED:
    read(rel) if not rel.endswith(".png") else (
        None if (ROOT / rel).is_file() else fail(f"missing required file: {rel}")
    )

toolchain = json.loads(read("toolchain.json") or "{}")
bundle = toolchain.get("bundle_identifier", "")
if bundle != "com.infinityball.pinballnova":
    fail(f"toolchain.json bundle_identifier is {bundle!r}")
for key in ("android_application_id", "msix_identity_name"):
    if toolchain.get(key) != bundle:
        fail(f"toolchain.json {key} != bundle_identifier")

# Unity version pin must stay on the 6000.3 line.
ver = read("ProjectSettings/ProjectVersion.txt")
m = re.search(r"m_EditorVersion:\s*(\S+)", ver)
if not m or not m.group(1).startswith(toolchain.get("unity_version_line", "6000.3") + "."):
    fail(f"ProjectVersion.txt editor version {m.group(1) if m else None!r} not on line {toolchain.get('unity_version_line')}")

# Bundle ID consistency across Standalone/Android/iPhone + MSIX package name.
ps = read("ProjectSettings/ProjectSettings.asset")
block_m = re.search(r"^  applicationIdentifier:\n((?:    [^\n]+\n)+)", ps, re.M)
ids = dict(re.findall(r"^    (Android|Standalone|iPhone): (\S+)$", block_m.group(1), re.M)) if block_m else {}
for platform in ("Android", "Standalone", "iPhone"):
    if ids.get(platform) != bundle:
        fail(f"ProjectSettings applicationIdentifier.{platform} = {ids.get(platform)!r}, want {bundle!r}")
mp = re.search(r"^  metroPackageName: (\S*)$", ps, re.M)
if not mp or mp.group(1) != bundle:
    fail(f"metroPackageName = {mp.group(1) if mp else None!r}, want {bundle!r}")
td = re.search(r"^  targetDevice: (\d+)$", ps, re.M)
if not td or td.group(1) != "2":
    fail("targetDevice must be 2 (iPhone + iPad)")
if re.search(r"^  resizableWindow: 1$", ps, re.M) is None:
    fail("resizableWindow must be 1 (desktop window resizing)")
if re.search(r"^  submitAnalytics: 0$", ps, re.M) is None:
    fail("submitAnalytics must be 0 (no telemetry)")
if re.search(r"^  activeInputHandler: 2$", ps, re.M) is None:
    fail("activeInputHandler must be 2 (Input System package + legacy off per fleet rules)")

# Package manifest: URP + Input System present, no prohibited engines/packages.
manifest = json.loads(read("Packages/manifest.json") or "{}").get("dependencies", {})
for pkg in ("com.unity.inputsystem", "com.unity.render-pipelines.universal", "com.unity.test-framework"):
    if pkg not in manifest:
        fail(f"Packages/manifest.json missing {pkg}")
for pkg in manifest:
    if re.search(r"ads|analytics|iap|purchasing|multiplayer|netcode|firebase", pkg, re.I):
        fail(f"prohibited package in manifest: {pkg}")

# Assembly definitions.
EXPECTED = {
    "Nova.Core": True,  # noEngineReferences
    "Nova.Physics": False,
    "Nova.Tables": False,
    "Nova.Input": False,
    "Nova.Presentation": False,
    "Nova.UI": False,
    "Nova.Persistence": False,
}
found = {}
for asm in (ROOT / "Assets").rglob("*.asmdef"):
    data = json.loads(asm.read_text(encoding="utf-8"))
    found[data["name"]] = data
    if not asm.with_name(asm.name + ".meta").is_file():
        fail(f"missing .meta for {asm.relative_to(ROOT)}")
for name, no_engine in EXPECTED.items():
    if name not in found:
        fail(f"missing asmdef {name}")
    elif no_engine and not found[name].get("noEngineReferences"):
        fail(f"{name} must set noEngineReferences=true")

# Every asset under Assets/ and every folder needs a tracked .meta (visible meta files mode).
for p in (ROOT / "Assets").rglob("*"):
    if p.suffix == ".meta" or p.name.startswith("."):
        continue
    if not p.with_name(p.name + ".meta").is_file():
        fail(f"missing .meta for {p.relative_to(ROOT)}")

# GUID uniqueness across .meta files.
guids = {}
for meta in list((ROOT / "Assets").rglob("*.meta")):
    g = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
    if not g:
        fail(f"no guid in {meta.relative_to(ROOT)}")
        continue
    if g.group(1) in guids:
        fail(f"duplicate guid {g.group(1)}: {meta.relative_to(ROOT)} and {guids[g.group(1)]}")
    guids[g.group(1)] = str(meta.relative_to(ROOT))

# Generated Unity folders must not be tracked.
try:
    tracked = subprocess.run(["git", "ls-files"], cwd=ROOT, capture_output=True, text=True, check=True).stdout.splitlines()
except Exception as exc:  # not a git checkout (e.g. exported tarball)
    tracked = []
    print(f"note: git ls-files unavailable ({exc}); skipping tracked-folder check")
for path in tracked:
    if re.match(r"^(Library|Temp|Obj|Build|Builds|Logs|UserSettings)/", path):
        fail(f"generated Unity path is tracked: {path}")

# .gitattributes must route Assets binaries to LFS.
ga = read(".gitattributes")
if "filter=lfs" not in ga:
    fail(".gitattributes has no Git LFS rules")

# CI workflow must reference secret NAMES only, windows-latest, and not embed secret values.
ci = read(".github/workflows/ci.yml")
for needle in ("windows-latest", "UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD", "StandaloneWindows64"):
    if needle not in ci:
        fail(f"ci.yml missing {needle}")

# Prohibited engines must not appear as project code.
for banned in ("pubspec.yaml", "package.json", "build.gradle.kts", "Package.swift"):
    if any(p.endswith(banned) for p in tracked):
        fail(f"prohibited engine/toolchain file tracked: {banned}")

if errors:
    print("STRUCTURE VALIDATION FAILED")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print(f"STRUCTURE VALIDATION OK ({len(REQUIRED)} required files, {len(found)} asmdefs, {len(guids)} guids, bundle {bundle})")
