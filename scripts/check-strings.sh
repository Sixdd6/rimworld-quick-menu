#!/usr/bin/env bash
# NFR-4 check: Keyed strings and key usage.
#  1. every "QuickMenu.*" key literal in src/ exists in mod/Languages/English/Keyed/*.xml
#  2. every Keyed key is used in src/ (or listed in scripts/keyed-allowlist.txt)
#  3. no hard-coded English passed to player-facing UI calls in src/QuickMenu (append "// strings-ok" to allow a line)
#  4. Keyed XML is well-formed with no duplicate keys
# Env overrides (for testing the check itself): SRC_DIR, KEYED_DIR, ALLOWLIST.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
SRC_DIR="${SRC_DIR:-src}" KEYED_DIR="${KEYED_DIR:-mod/Languages/English/Keyed}" \
  ALLOWLIST="${ALLOWLIST:-scripts/keyed-allowlist.txt}" exec python3 -I - <<'PY'
import os, re, sys
import xml.etree.ElementTree as ET

src, keyed, allow = os.environ["SRC_DIR"], os.environ["KEYED_DIR"], os.environ["ALLOWLIST"]
errors = []

defined = {}
for name in sorted(os.listdir(keyed)):
    if not name.endswith(".xml"):
        continue
    path = os.path.join(keyed, name)
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as e:
        errors.append(f"{path}: not well-formed: {e}")
        continue
    for el in root:
        if not isinstance(el.tag, str):
            continue
        if not el.tag.startswith("QuickMenu."):
            errors.append(f"{path}: key '{el.tag}' lacks the QuickMenu. prefix")
        if el.tag in defined:
            errors.append(f"{path}: duplicate key '{el.tag}' (first in {defined[el.tag]})")
        defined.setdefault(el.tag, path)

allowed = set()
if os.path.exists(allow):
    for line in open(allow, encoding="utf-8"):
        line = line.split("#", 1)[0].strip()
        if line:
            allowed.add(line)

lit_re = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
key_re = re.compile(r'^QuickMenu\.[A-Za-z0-9_.]*$')
ui_re = re.compile(r'\b(Widgets\.\w*(Label|Button\w*|Checkbox\w*|TextField\w*|Dropdown\w*)|Messages\.Message|'
                   r'TooltipHandler\.\w+|Dialog_MessageBox|Text\.\w*Draw\w*|new\s+Dialog_\w+|'
                   r'optionalTitle|windowTitle|FloatMenuOption|Messages\.\w+)\s*\(')
used = set(); prefixes = set(); used_in_src = False
for d, _, files in os.walk(src):
    if "/obj" in d or "/bin" in d or os.sep + "obj" in d or os.sep + "bin" in d:
        continue
    for f in files:
        if not f.endswith(".cs"):
            continue
        p = os.path.join(d, f)
        game = "QuickMenu.Core" not in p
        for n, line in enumerate(open(p, encoding="utf-8"), 1):
            code = line.split("//", 1)[0]
            for m in lit_re.finditer(code):
                s = m.group(1)
                if key_re.match(s) and "strings-ok" not in line:
                    if s.endswith(".") or s.endswith("QuickMenu"):
                        prefixes.add(s)
                    else:
                        used.add(s)
                        if s not in defined:
                            errors.append(f"{p}:{n}: key '{s}' is not defined in Keyed XML")
            if game and "strings-ok" not in line and ui_re.search(code):
                for m in lit_re.finditer(code):
                    s = m.group(1)
                    if key_re.match(s) or not re.search(r"[A-Za-z]{2}", s):
                        continue
                    errors.append(f"{p}:{n}: hard-coded player-facing string \"{s}\" (use a Keyed key)")

for k, p in defined.items():
    if k in used or k in allowed or any(k.startswith(x) for x in prefixes):
        continue
    errors.append(f"{p}: key '{k}' is unused in src/ (remove it or add to {allow})")
for k in sorted(allowed - set(defined)):
    errors.append(f"{allow}: allowlisted key '{k}' is not defined")

if errors:
    print("Keyed-string check FAILED (NFR-4):", file=sys.stderr)
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
print(f"Keyed-string check OK: {len(defined)} keys, {len(used)} key literals used.")
PY
