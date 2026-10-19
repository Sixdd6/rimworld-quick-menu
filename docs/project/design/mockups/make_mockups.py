#!/usr/bin/env python3
"""Generate the direction mockups for docs/project/design.md.

Run:  python3 -I docs/project/design/mockups/make_mockups.py
Needs ImageMagick 7 with librsvg (``magick``) to export PNGs.

The geometry functions below follow design.md section 1 exactly (ring radius
R_k = k*P, ring capacity floor(pi / asin(1 / 2k)), grid cell pitch P), so the
mockups double as a visual check of the layout rules. Icons are neutral
placeholders: the real menu shows the game's own designator icons.
"""
import math
import os
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(HERE)  # docs/project/design/

# ---- design tokens (keep in sync with design.md section 2) -----------------
T = {
    "accent": "#38BDF8",
    "discCentre": (0, 0, 0, 0.85),
    "discEdge": (0, 0, 0, 0.55),
    "rimOutline": "rgba(0,0,0,0.6)",
    "slotBg": "#1E2328",
    "slotBgHover": "#2A3540",
    "slotBgPressed": "#15191D",
    "slotBorder": "#56626E",
    "emptyBorder": "#8A96A0",
    "text": "#FFFFFF",
    "textMuted": "#A7B0B8",
    "danger": "#FF7A70",
    "windowBg": "#15191D",
    "sectionBg": "#1F242A",
    "windowBorder": "#5A626B",
    "buttonBg": "#5E4B33",
    "buttonBorder": "#2A2116",
    "plate": "rgba(0,0,0,0.80)",
    "unavailableIcon": "#808080",
}
FONT = "font-family:'Noto Sans','DejaVu Sans',sans-serif"

B = 56  # button (slot) diameter, UI px
G = 8   # slot gap
P = B + G  # pitch
RIM_PAD = 12
HUB = B  # hub diameter


def ring_capacity(k):
    """Slots that fit on ring k (radius k*P) with centre spacing >= P."""
    return int(math.floor(math.pi / math.asin(1.0 / (2 * k)) + 1e-9))  # epsilon: k=1 is 5.999... in floats


def wheel_slots(rings, cx, cy, pitch=P):
    out = []
    for k in range(1, rings + 1):
        n = ring_capacity(k)
        r = k * pitch
        for i in range(n):
            a = math.radians(-90 + i * 360.0 / n)
            out.append((k, i, cx + r * math.cos(a), cy + r * math.sin(a)))
    return out


def disc_radius(rings, b=B, pitch=P):
    return rings * pitch + b / 2 + RIM_PAD


# ---- svg helpers ---------------------------------------------------------------
def rgba(t):
    return "rgba(%d,%d,%d,%.2f)" % t


def defs():
    return f"""<defs>
  <radialGradient id="disc" cx="50%" cy="50%" r="50%">
    <stop offset="0%" stop-color="{rgba(T['discCentre'])}"/>
    <stop offset="100%" stop-color="{rgba(T['discEdge'])}"/>
  </radialGradient>
  <pattern id="terrain" width="120" height="120" patternUnits="userSpaceOnUse">
    <rect width="120" height="120" fill="#6E5C3F"/>
    <rect x="10" y="14" width="46" height="30" fill="#7C6A4A"/>
    <rect x="70" y="60" width="40" height="44" fill="#5F7A3A"/>
    <rect x="20" y="70" width="30" height="30" fill="#655437"/>
  </pattern>
  <filter id="shadow" x="-20%" y="-20%" width="140%" height="140%">
    <feDropShadow dx="0" dy="2" stdDeviation="3" flood-color="#000" flood-opacity="0.6"/>
  </filter>
</defs>"""


def text(x, y, s, size=13, fill=None, anchor="start", weight="normal", extra=""):
    fill = fill or T["text"]
    s = s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    return (f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" fill="{fill}" '
            f'text-anchor="{anchor}" font-weight="{weight}" style="{FONT}" {extra}>{s}</text>')


def disc(cx, cy, r):
    # disc fill, then 1px dark outline, 2px accent rim, 1px dark inner outline
    return (f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="url(#disc)"/>'
            f'<circle cx="{cx}" cy="{cy}" r="{r - 1}" fill="none" stroke="{T["rimOutline"]}" stroke-width="4"/>'
            f'<circle cx="{cx}" cy="{cy}" r="{r - 1}" fill="none" stroke="{T["accent"]}" stroke-width="2"/>')


def hub_geom(b=B):
    e = min(28, max(18, round(0.36 * b)))
    ey = max(b / 2 - e / 2 - 2, e / 2 + 6)
    return e, ey


def hub(cx, cy, mode="use", hover=None, b=B):
    """Close/edit hub (design.md S4a). mode: use|edit. hover: None|main|sub."""
    e, ey = hub_geom(b)
    main_hover = hover == "main"
    sub_hover = hover == "sub"
    edit = mode == "edit"
    fill = T["slotBgHover"] if main_hover else T["slotBg"]
    ring = T["accent"] if (main_hover or edit) else T["slotBorder"]
    rw = 2 if (main_hover or edit) else 1
    out = [f'<circle cx="{cx}" cy="{cy}" r="{b / 2 - rw / 2}" fill="{fill}" fill-opacity="{0.92 if main_hover else 0.6}" stroke="{ring}" stroke-width="{rw}"/>']
    gy = cy - 0.12 * b
    g = 0.11 * b
    col = T["text"] if (main_hover or edit) else T["textMuted"]
    if edit:  # check (Done)
        out.append(f'<path d="M{cx - g} {gy} L{cx - g * .3} {gy + g * .7} L{cx + g} {gy - g * .6}" stroke="{col}" stroke-width="2.5" fill="none"/>')
    else:  # x (Close)
        out.append(f'<path d="M{cx - g} {gy - g} L{cx + g} {gy + g} M{cx + g} {gy - g} L{cx - g} {gy + g}" stroke="{col}" stroke-width="2.2"/>')
    sx, sy = cx, cy + ey
    out.append(f'<circle cx="{sx}" cy="{sy}" r="{e / 2 - 0.5}" fill="{T["slotBgHover"] if sub_hover else T["slotBg"]}" stroke="{T["accent"]}" stroke-width="{2 if sub_hover else 1}"/>')
    k = e * 0.3
    if edit:  # plus (Add)
        out.append(f'<path d="M{sx - k} {sy} H{sx + k} M{sx} {sy - k} V{sy + k}" stroke="{T["text"]}" stroke-width="2"/>')
    else:  # pencil, pre-rotated path (no rotation at draw time)
        out.append(f'<path d="M{sx - k} {sy + k} L{sx - k * .9} {sy + k * .45} L{sx + k * .55} {sy - k * 1.0} L{sx + k} {sy - k * .55} L{sx - k * .45} {sy + k * .9} Z" fill="{T["text"]}"/>')
    return "".join(out)


GLYPHS = {
    # name: (colour, shape)
    "wall": ("#B9A27A", "bricks"), "door": ("#9C7B52", "door"), "chair": ("#A0784C", "chair"),
    "bed": ("#7FA0C8", "bed"), "mine": ("#C8C8C8", "pick"), "smelter": ("#C07040", "box"),
    "decon": ("#E06050", "x"), "cancel": ("#E0E0E0", "x"), "table": ("#A0784C", "table"),
    "lamp": ("#F2D16B", "lamp"), "floor": ("#8A8A8A", "floor"), "shelf": ("#9C8A6A", "shelf"),
    "harvest": ("#7CC060", "leaf"), "wood": ("#A0784C", "log"), "steel": ("#9AA4AE", "ingot"),
    "granite": ("#8C8480", "block"), "marble": ("#D8D4CC", "block"), "plasteel": ("#9FC8C8", "ingot"),
}


def glyph(name, x, y, s, grey=False, alpha=1.0):
    col, shape = GLYPHS[name]
    if grey:
        col = T["unavailableIcon"]
    h = s / 2
    st = f'fill="{col}" opacity="{alpha}"'
    if shape == "bricks":
        r = "".join(f'<rect x="{x - h + (0 if row % 2 == 0 else s * .18)}" y="{y - h + row * s * .25 + 2}" width="{s * .8}" height="{s * .2}" {st}/>' for row in range(4))
        return r
    if shape == "door":
        return f'<rect x="{x - s * .28}" y="{y - h}" width="{s * .56}" height="{s}" rx="3" {st}/><circle cx="{x + s * .15}" cy="{y}" r="2" fill="#333"/>'
    if shape == "chair":
        return (f'<rect x="{x - s * .3}" y="{y - h}" width="{s * .12}" height="{s}" {st}/>'
                f'<rect x="{x - s * .3}" y="{y}" width="{s * .6}" height="{s * .12}" {st}/>'
                f'<rect x="{x + s * .2}" y="{y}" width="{s * .1}" height="{s * .5}" {st}/>')
    if shape == "bed":
        return f'<rect x="{x - s * .3}" y="{y - h}" width="{s * .6}" height="{s}" rx="4" {st}/><rect x="{x - s * .22}" y="{y - h + 3}" width="{s * .44}" height="{s * .2}" rx="3" fill="#EEE" opacity="{alpha}"/>'
    if shape == "pick":
        return f'<path d="M{x - h} {y - h * .3} Q{x} {y - h * 1.1} {x + h} {y - h * .3}" stroke="{col}" stroke-width="4" fill="none" opacity="{alpha}"/><rect x="{x - 2}" y="{y - h * .6}" width="4" height="{s * .8}" {st}/>'
    if shape == "box":
        return f'<rect x="{x - h}" y="{y - h * .8}" width="{s}" height="{s * .8}" rx="3" {st}/><rect x="{x - h * .4}" y="{y - h * .3}" width="{s * .4}" height="{s * .3}" fill="#FFB060" opacity="{alpha}"/>'
    if shape == "x":
        return f'<path d="M{x - h} {y - h} L{x + h} {y + h} M{x + h} {y - h} L{x - h} {y + h}" stroke="{col}" stroke-width="5" opacity="{alpha}"/>'
    if shape == "table":
        return f'<rect x="{x - h}" y="{y - h * .5}" width="{s}" height="{s * .16}" {st}/><rect x="{x - h + 3}" y="{y - h * .5}" width="4" height="{s * .6}" {st}/><rect x="{x + h - 7}" y="{y - h * .5}" width="4" height="{s * .6}" {st}/>'
    if shape == "lamp":
        return f'<circle cx="{x}" cy="{y - h * .2}" r="{s * .3}" {st}/><rect x="{x - 3}" y="{y + h * .3}" width="6" height="{s * .3}" fill="#888" opacity="{alpha}"/>'
    if shape == "floor":
        return "".join(f'<rect x="{x - h + c * s / 3}" y="{y - h + r_ * s / 3}" width="{s / 3 - 2}" height="{s / 3 - 2}" {st}/>' for c in range(3) for r_ in range(3))
    if shape == "shelf":
        return "".join(f'<rect x="{x - h}" y="{y - h + i * s / 3}" width="{s}" height="{s * .08}" {st}/>' for i in range(4))
    if shape == "leaf":
        return f'<ellipse cx="{x}" cy="{y}" rx="{s * .25}" ry="{h}" transform="rotate(35 {x} {y})" {st}/>'
    if shape == "log":
        return f'<rect x="{x - h}" y="{y - s * .18}" width="{s}" height="{s * .36}" rx="{s * .18}" {st}/><circle cx="{x + h - s * .18}" cy="{y}" r="{s * .14}" fill="#D8B888" opacity="{alpha}"/>'
    if shape == "ingot":
        return f'<path d="M{x - h} {y + h * .4} L{x - h * .6} {y - h * .4} L{x + h * .6} {y - h * .4} L{x + h} {y + h * .4} Z" {st}/>'
    if shape == "block":
        return f'<rect x="{x - h * .8}" y="{y - h * .6}" width="{s * .8}" height="{s * .6}" {st}/>'
    return ""


def slot(cx, cy, b=B, icon=None, state="normal", dropdown=False, stuff=None, locked=False,
         insert=False, alpha=1.0, lastused=False):
    """Draw one slot. state: normal|hover|pressed|empty|empty-edit|target|origin."""
    r = b / 2
    out = [f'<g opacity="{alpha}">']
    if state in ("empty", "empty-edit", "origin"):
        out.append(f'<circle cx="{cx}" cy="{cy}" r="{r - .5}" fill="{T["slotBg"]}" fill-opacity="0.7" '
                   f'stroke="{T["emptyBorder"]}" stroke-width="1" stroke-dasharray="{"4 3" if state != "empty" else "none"}"/>')
        if state == "empty-edit":
            out.append(f'<path d="M{cx - 7} {cy} H{cx + 7} M{cx} {cy - 7} V{cy + 7}" stroke="{T["textMuted"]}" stroke-width="2"/>')
    else:
        fill = {"normal": T["slotBg"], "hover": T["slotBgHover"], "pressed": T["slotBgPressed"],
                "target": T["slotBgHover"]}[state]
        stroke = T["accent"] if state in ("hover", "target") or insert or lastused else T["slotBorder"]
        sw = 2 if state in ("hover", "target") else (3 if insert or lastused else 1)
        out.append(f'<circle cx="{cx}" cy="{cy}" r="{r - sw / 2}" fill="{fill}" fill-opacity="0.92" stroke="{stroke}" stroke-width="{sw}"/>')
        if state == "target" or insert:
            out.append(f'<circle cx="{cx}" cy="{cy}" r="{r - 3}" fill="{T["accent"]}" fill-opacity="0.22"/>')
        if state == "hover":
            out.append(f'<circle cx="{cx}" cy="{cy}" r="{r - 2}" fill="#FFFFFF" fill-opacity="0.08"/>')
    if icon:
        out.append(glyph(icon, cx, cy, b * 0.75 * 0.62, grey=locked, alpha=0.75 if locked else 1))
    d = r * math.cos(math.radians(45))
    if dropdown:  # 1:30 corner triangle (game's PlusOptions badge)
        bx, by, s = cx + d, cy - d, b * 0.22
        out.append(f'<path d="M{bx - s / 2} {by - s / 2} H{bx + s / 2} V{by + s / 2} Z" fill="#C8CDD2" stroke="#000" stroke-opacity=".6" stroke-width="1"/>')
    if stuff:  # 4:30 material dot
        out.append(f'<circle cx="{cx + d}" cy="{cy + d}" r="{b * 0.09}" fill="{stuff}" stroke="#000" stroke-opacity=".7" stroke-width="1"/>')
    if locked:  # 7:30 lock badge (game's LockedMonochrome, tinted textMuted)
        lx, ly = cx - d, cy + d
        out.append(f'<rect x="{lx - 6}" y="{ly - 3}" width="12" height="9" rx="1.5" fill="{T["textMuted"]}" stroke="#000" stroke-opacity=".6"/>'
                   f'<path d="M{lx - 3.5} {ly - 3} V{ly - 6} A3.5 3.5 0 0 1 {lx + 3.5} {ly - 6} V{ly - 3}" stroke="{T["textMuted"]}" stroke-width="2" fill="none"/>')
    if insert:  # 10:30 insert caret
        ix, iy = cx - d, cy - d
        out.append(f'<path d="M{ix - 7} {iy - 6} L{ix + 3} {iy} L{ix - 7} {iy + 6} Z" fill="{T["accent"]}" stroke="#000" stroke-opacity=".6"/>')
    out.append("</g>")
    return "".join(out)


def plate(cx, y, s, size=12, anchor_bottom=True):
    w = len(s) * size * 0.55 + 14
    h = size + 10
    top = y - h if anchor_bottom else y
    return (f'<rect x="{cx - w / 2}" y="{top}" width="{w}" height="{h}" rx="3" fill="{T["plate"]}"/>'
            + text(cx, top + h - 7, s, size=size, anchor="middle"))


def round_button(cx, cy, kind, d=28, hover=False):
    out = [f'<circle cx="{cx}" cy="{cy}" r="{d / 2}" fill="{T["slotBgHover"] if hover else T["slotBg"]}" stroke="{T["accent"]}" stroke-width="2" filter="url(#shadow)"/>']
    if kind == "edit":  # pencil (our icon)
        out.append(f'<g transform="rotate(45 {cx} {cy})"><rect x="{cx - 2.5}" y="{cy - 8}" width="5" height="12" fill="{T["text"]}"/>'
                   f'<path d="M{cx - 2.5} {cy + 4} L{cx} {cy + 8} L{cx + 2.5} {cy + 4} Z" fill="{T["text"]}"/></g>')
    elif kind == "done":
        out.append(f'<path d="M{cx - 7} {cy} L{cx - 2} {cy + 5} L{cx + 7} {cy - 5}" stroke="{T["text"]}" stroke-width="2.5" fill="none"/>')
    elif kind == "add":
        out.append(f'<path d="M{cx - 7} {cy} H{cx + 7} M{cx} {cy - 7} V{cy + 7}" stroke="{T["text"]}" stroke-width="2.5"/>')
    return "".join(out)


def cursor(x, y):
    return (f'<path d="M{x} {y} L{x} {y + 18} L{x + 5} {y + 13} L{x + 9} {y + 21} L{x + 12} {y + 19} '
            f'L{x + 8} {y + 12} L{x + 14} {y + 12} Z" fill="#FFF" stroke="#000" stroke-width="1.2"/>')


def svg(w, h, body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">{defs()}{body}</svg>'


def write(name, content):
    p = os.path.join(HERE, name + ".svg")
    with open(p, "w") as f:
        f.write(content)
    png = os.path.join(OUT, name + ".png")
    subprocess.run(["magick", "-background", "none", "RSVG:" + p, png], check=True)
    print("wrote", png)


# ---- mockup 1: wheel states ------------------------------------------------------
def mock_wheel():
    W, H = 1200, 720
    cx, cy, rings = 400, 380, 2
    body = [f'<rect width="{W}" height="{H}" fill="url(#terrain)"/>',
            f'<rect x="0" y="0" width="{W}" height="{H}" fill="#000" opacity="0"/>',
            f'<path d="M0 0 H330 L250 260 L0 300 Z" fill="#F4F6F8" opacity=".95"/>',  # snow: worst case
            text(20, 30, "Snow / bright terrain (worst case for the disc edge)", 12, fill="#333")]
    D = disc_radius(rings)
    body.append(disc(cx, cy, D))
    plan = {0: ("wall", {}), 1: ("door", {"state": "hover"}), 2: ("chair", {"dropdown": True}),
            3: ("bed", {"stuff": "#A0784C"}), 4: ("mine", {}), 5: ("smelter", {"locked": True}),
            6: ("decon", {}), 7: ("cancel", {}), 8: ("table", {"stuff": "#9AA4AE"}), 9: ("lamp", {}),
            10: (None, {"state": "empty"}), 11: (None, {"state": "empty"}), 12: ("floor", {"dropdown": True}),
            13: (None, {"state": "empty"}), 14: ("shelf", {"stuff": "#8C8480"}), 15: (None, {"state": "empty"}),
            16: ("harvest", {}), 17: (None, {"state": "empty"})}
    hover_pos = None
    for idx, (k, i, x, y) in enumerate(wheel_slots(rings, cx, cy)):
        icon, kw = plan[idx]
        body.append(slot(x, y, icon=icon, **kw))
        if kw.get("state") == "hover":
            hover_pos = (x, y)
    hx, hy = hover_pos
    body.append(hub(cx, cy))
    body.append(plate(cx, cy + D + 8, "Door (wood)", anchor_bottom=False))
    body.append(cursor(hx + 6, hy + 4))
    # hub states inset
    ix, iy = 20, 330
    body.append(f'<rect x="{ix}" y="{iy}" width="190" height="370" rx="6" fill="{T["windowBg"]}" stroke="{T["windowBorder"]}"/>')
    body.append(text(ix + 12, iy + 24, "Centre hub (S4a)", 14, weight="bold"))
    for n, (mode, hv, l1, l2) in enumerate([("use", None, "Close (main area)", "Edit (small, below)"),
                                            ("use", "sub", "Edit hovered", "click only; release ignored"),
                                            ("edit", None, "Edit mode:", "Done (main) + Add (small)")]):
        yy = iy + 80 + n * 105
        body.append(hub(ix + 44, yy, mode=mode, hover=hv))
        body.append(text(ix + 82, yy - 4, l1, 12, weight="bold"))
        body.append(text(ix + 82, yy + 12, l2, 10, fill=T["textMuted"]))
    body.append(text(cx, cy + D + 62, "Wheel, Fixed, 2 rings = 6 + 12 slots, button 56, gap 8", 13, anchor="middle", extra='stroke="#000" stroke-width="3" paint-order="stroke"'))

    # legend
    lx, ly = 760, 60
    body.append(f'<rect x="{lx - 20}" y="{ly - 40}" width="440" height="650" rx="6" fill="{T["windowBg"]}" stroke="{T["windowBorder"]}"/>')
    body.append(text(lx, ly - 12, "Slot states (same component in wheel and grid)", 15, weight="bold"))
    rows = [
        ("Normal", dict(icon="wall"), "slotBg, 1px slotBorder, game icon at 75%"),
        ("Hover", dict(icon="door", state="hover"), "slotBgHover, 2px accent rim, +8% white; label below menu"),
        ("Pressed", dict(icon="door", state="pressed"), "slotBgPressed while mouse is down"),
        ("Sub-menu", dict(icon="chair", dropdown=True), "sub-menu corner badge (our mask) at 1:30"),
        ("Material", dict(icon="bed", stuff="#A0784C"), "dot in last-used material colour at 4:30"),
        ("Unavailable", dict(icon="smelter", locked=True), "grey icon + lock badge at 7:30; reason in tooltip + message"),
        ("Empty (Fixed)", dict(state="empty"), "70% slotBg, 1px emptyBorder; no hover"),
        ("Empty (edit)", dict(state="empty-edit"), "dashed border + plus; click = insert point"),
        ("Insert point", dict(icon="wall", insert=True), "3px accent rim, accent tint, caret at 10:30"),
        ("Drop target", dict(icon="lamp", state="target"), "accent rim + tint while dragging over it"),
        ("Drag origin", dict(state="origin"), "dashed ghost; dragged icon follows the mouse"),
    ]
    for n, (name, kw, desc) in enumerate(rows):
        y = ly + 30 + n * 56
        body.append(slot(lx + 28, y, **kw))
        body.append(text(lx + 70, y - 4, name, 14, weight="bold"))
        body.append(text(lx + 70, y + 14, desc, 12, fill=T["textMuted"]))
    write("mockup-wheel-states", svg(W, H, "".join(body)))


# ---- mockup 2: material sub-ring + grid edit mode -------------------------------
def mock_sub_and_grid():
    W, H = 1200, 720
    body = [f'<rect width="{W}" height="{H}" fill="url(#terrain)"/>']
    # left: main wheel dimmed, satellite on the bed slot
    cx, cy, rings = 300, 300, 2
    D = disc_radius(rings)
    main = [disc(cx, cy, D)]
    icons = ["wall", "door", "chair", "bed", "mine", "smelter", "decon", "cancel", "table", "lamp", None, None,
             "floor", None, "shelf", None, "harvest", None]
    slots = wheel_slots(rings, cx, cy)
    for idx, (k, i, x, y) in enumerate(slots):
        ic = icons[idx]
        main.append(slot(x, y, icon=ic, state="normal" if ic else "empty"))
    main.append(hub(cx, cy))
    body.append(f'<g opacity="0.45">{"".join(main)}</g>')
    px, py = slots[3][2], slots[3][3]  # bed, ring 1 bottom
    mats = [("wood", "Wood (last used)", True), ("steel", "Steel", False), ("granite", "Granite blocks", False),
            ("marble", "Marble blocks", False), ("plasteel", "Plasteel", False)]
    sr = P + B / 2 + RIM_PAD
    body.append(disc(px, py, sr))
    body.append(slot(px, py, icon="bed", insert=False, state="pressed"))
    body.append(f'<circle cx="{px}" cy="{py}" r="{B / 2 - 1}" fill="none" stroke="{T["accent"]}" stroke-width="2"/>')
    for j, (ic, label, last) in enumerate(mats):
        a = math.radians(-90 + j * 360 / len(mats))
        x, y = px + P * math.cos(a), py + P * math.sin(a)
        body.append(slot(x, y, icon=ic, state="hover" if j == 1 else "normal", lastused=last))
        if j == 1:
            body.append(plate(px, py + sr + 8, label, anchor_bottom=False))
            body.append(cursor(x + 4, y + 6))
    body.append(text(300, 640, "Material sub-ring: centred on the parent slot, main wheel dimmed to 45%.", 13, anchor="middle", extra='stroke="#000" stroke-width="3" paint-order="stroke"'))
    body.append(text(300, 660, "Last-used material first (top) with 3px accent rim. Hub = parent; click hub or right-click = back.", 13, anchor="middle", extra='stroke="#000" stroke-width="3" paint-order="stroke"'))

    # right: grid 3x5 edit mode
    gx, gy, rows, cols = 880, 250, 3, 5
    pad = RIM_PAD
    w = cols * P - G + 2 * pad
    h = rows * P - G + 2 * pad
    body.append(f'<rect x="{gx - w / 2}" y="{gy - h / 2}" width="{w}" height="{h}" rx="10" fill="rgba(0,0,0,0.75)" stroke="{T["rimOutline"]}" stroke-width="4"/>'
                f'<rect x="{gx - w / 2}" y="{gy - h / 2}" width="{w}" height="{h}" rx="10" fill="none" stroke="{T["accent"]}" stroke-width="2"/>')
    gicons = ["wall", "door", "chair", "bed", "mine", "smelter", "decon", None, "table", "lamp", None, "floor", "shelf", "harvest"]
    n = 0
    for r_ in range(rows):
        for c in range(cols):
            x = gx + (c - (cols - 1) / 2) * P
            y = gy + (r_ - (rows - 1) / 2) * P
            if r_ == rows // 2 and c == cols // 2:
                hubxy = (x, y)
                continue
            ic = gicons[n]
            kw = {}
            if n == 2:
                kw = dict(insert=True, dropdown=True)
            if n == 8:
                kw = dict(state="target")
            if n == 5:
                kw = dict(locked=True)
            if ic is None:
                kw = dict(state="empty-edit")
            if n == 4:
                kw = dict(state="origin"); ic = None
            body.append(slot(x, y, icon=ic, **kw))
            if n == 8:
                tx, ty = x, y
            n += 1
    # drag ghost following cursor near target
    body.append(slot(tx + 34, ty + 30, icon="mine", alpha=0.8, b=B * 0.9))
    body.append(cursor(tx + 44, ty + 40))
    # hub in edit mode: Done + Add
    body.append(hub(hubxy[0], hubxy[1], mode="edit"))
    # help panel at the top centre of the screen
    hy = 20
    help_lines = ["Edit mode (Fixed): click a slot to choose where the next pin goes.  12 of 14 slots used",
                  "Add from list, or Ctrl+click any build or order button.",
                  "Drag a slot onto another to swap them. Right-click to clear a slot.",
                  "Done (centre) or Esc to finish. Add (small plus in the centre) opens the list."]
    hw = 520
    body.append(f'<rect x="{gx - hw / 2}" y="{hy}" width="{hw}" height="{len(help_lines) * 18 + 16}" rx="4" fill="{T["plate"]}"/>')
    for i, l in enumerate(help_lines):
        body.append(text(gx - hw / 2 + 12, hy + 22 + i * 18, l, 12, fill=T["text"] if i == 0 else T["textMuted"]))
    body.append(text(gx, 640, "Grid, Fixed, 3 rows x 5 columns, centre cell = hub (14 slots), edit mode.", 13, anchor="middle", extra='stroke="#000" stroke-width="3" paint-order="stroke"'))
    body.append(text(gx, 660, "Hub = Done + Add. Help panel at top of screen.", 13, anchor="middle", extra='stroke="#000" stroke-width="3" paint-order="stroke"'))
    write("mockup-subring-grid-edit", svg(W, H, "".join(body)))


# ---- mockup 3: settings window + picker -----------------------------------------
def vbutton(x, y, w, h, label, active=False, accent=False, disabled=False):
    fill = "#2A3540" if accent else T["buttonBg"]
    stroke = T["accent"] if (accent or active) else T["buttonBorder"]
    fc = T["textMuted"] if disabled else T["text"]
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="2" fill="{fill}" stroke="{stroke}" stroke-width="{2 if active or accent else 1}" opacity="{0.5 if disabled else 1}"/>'
            + text(x + w / 2, y + h / 2 + 5, label, 13, fill=fc, anchor="middle"))


def radio(x, y, label, on):
    return (f'<circle cx="{x + 8}" cy="{y}" r="8" fill="#222" stroke="#BBB" stroke-width="1.5"/>'
            + (f'<circle cx="{x + 8}" cy="{y}" r="4" fill="{T["accent"]}"/>' if on else "")
            + text(x + 24, y + 5, label, 13))


def mock_settings_picker():
    W, H = 1560, 820
    body = [f'<rect width="{W}" height="{H}" fill="#0B0D0F"/>']
    # settings window (Dialog_ModSettings content area ~ 864 x 584)
    wx, wy, ww, wh = 30, 40, 900, 700
    body.append(f'<rect x="{wx}" y="{wy}" width="{ww}" height="{wh}" fill="{T["windowBg"]}" stroke="{T["windowBorder"]}" filter="url(#shadow)"/>')
    body.append(text(wx + 18, wy + 34, "Quick Menu", 20, weight="bold"))
    cx0, cy0 = wx + 18, wy + 60
    colw = 420
    # left column: opening
    y = cy0 + 10
    body.append(text(cx0, y, "Opening the menu", 15, weight="bold")); y += 10
    body.append(f'<line x1="{cx0}" y1="{y}" x2="{cx0 + colw}" y2="{y}" stroke="{T["windowBorder"]}"/>'); y += 28
    body.append(text(cx0, y + 5, "Open menu with", 13))
    body.append(vbutton(cx0 + 170, y - 13, 190, 28, "Mouse 4"))
    body.append(vbutton(cx0 + 366, y - 13, 28, 28, "x"))
    y += 40
    body.append(vbutton(cx0 + 170, y - 13, 224, 28, "Use middle mouse instead"))
    y += 30
    for l in ["Mice do not report side buttons. If Mouse 4", "does nothing, use middle mouse or bind a key.", "Middle mouse also drags the camera while held."]:
        body.append(text(cx0 + 170, y, l, 11, fill=T["textMuted"])); y += 15
    y += 8
    body.append(text(cx0, y, "Click the binding: capture dialog (ADR-0003)", 11, fill=T["textMuted"]))
    body.append(f'<rect x="{cx0 + 170}" y="{y - 16}" width="224" height="28" rx="2" fill="#2A3540" stroke="{T["accent"]}" stroke-width="2"/>'
                + text(cx0 + 282, y + 3, "Left/right click rejected", 12, anchor="middle"))
    y += 40
    body.append(text(cx0, y + 5, "Trigger mode", 13))
    body.append(radio(cx0 + 170, y, "Press to open, press again to close", True)); y += 26
    body.append(radio(cx0 + 170, y, "Hold, release over a button", False)); y += 44
    body.append(text(cx0, y, "Pinning", 15, weight="bold")); y += 10
    body.append(f'<line x1="{cx0}" y1="{y}" x2="{cx0 + colw}" y2="{y}" stroke="{T["windowBorder"]}"/>'); y += 28
    body.append(text(cx0, y + 5, "Pin with", 13))
    body.append(vbutton(cx0 + 170, y - 13, 110, 28, "Alt  v"))
    body.append(text(cx0 + 290, y + 5, "+ click on a build or order", 12, fill=T["textMuted"])); y += 30
    for l in ["Dubs Mint Menus is installed, so Alt is the", "default and Ctrl+click stays with Mint."]:
        body.append(text(cx0 + 170, y, l, 11, fill=T["textMuted"])); y += 15
    y += 16
    body.append(vbutton(cx0 + 170, y - 13, 160, 28, "Clear all pins..."))
    # right column: layout
    rx = wx + 470
    y = cy0 + 10
    body.append(text(rx, y, "Layout", 15, weight="bold")); y += 10
    body.append(f'<line x1="{rx}" y1="{y}" x2="{rx + 410}" y2="{y}" stroke="{T["windowBorder"]}"/>'); y += 28
    body.append(text(rx, y + 5, "Shape", 13))
    body.append(vbutton(rx + 130, y - 13, 100, 28, "Wheel", active=True, accent=True))
    body.append(vbutton(rx + 236, y - 13, 100, 28, "Grid"))
    y += 40
    body.append(text(rx, y + 5, "Size", 13))
    body.append(radio(rx + 130, y, "Fixed", True)); body.append(radio(rx + 220, y, "Auto", False)); y += 40
    body.append(text(rx, y + 5, "Rings", 13))
    body.append(vbutton(rx + 130, y - 13, 28, 28, "-")); body.append(text(rx + 178, y + 5, "2", 14, anchor="middle"))
    body.append(vbutton(rx + 198, y - 13, 28, 28, "+")); body.append(text(rx + 240, y + 5, "18 slots", 12, fill=T["textMuted"])); y += 40
    body.append(text(rx, y + 5, "Button size", 13))
    body.append(f'<rect x="{rx + 130}" y="{y - 2}" width="220" height="4" fill="#666"/><rect x="{rx + 130 + 220 * (56 - 36) / 60 - 5}" y="{y - 8}" width="10" height="16" fill="#CCC"/>')
    body.append(text(rx + 370, y + 5, "56", 13)); y += 30
    # preview box
    pvw, pvh = 410, 270
    body.append(f'<rect x="{rx}" y="{y}" width="{pvw}" height="{pvh}" fill="#0F1214" stroke="{T["windowBorder"]}"/>')
    body.append(text(rx + 8, y + 18, "Preview (50%)", 11, fill=T["textMuted"]))
    sc = 0.5
    pcx, pcy = rx + pvw / 2, y + pvh / 2 + 6
    bP = P * sc
    Dp = 2 * bP + B * sc / 2 + RIM_PAD * sc
    body.append(disc(pcx, pcy, Dp))
    for (k, i, x, yy) in wheel_slots(2, pcx, pcy, pitch=bP):
        body.append(f'<circle cx="{x}" cy="{yy}" r="{B * sc / 2}" fill="{T["slotBg"]}" stroke="{T["slotBorder"]}"/>')
    body.append(f'<circle cx="{pcx}" cy="{pcy}" r="{B * sc / 2}" fill="{T["slotBg"]}" fill-opacity=".6" stroke="{T["slotBorder"]}"/>')
    y += pvh + 30
    body.append(vbutton(wx + 18, wy + wh - 48, 160, 30, "Reset to defaults"))

    # picker window
    px, py, pw, ph = 980, 60, 540, 680
    body.append(f'<rect x="{px}" y="{py}" width="{pw}" height="{ph}" fill="{T["windowBg"]}" stroke="{T["windowBorder"]}" filter="url(#shadow)"/>')
    body.append(text(px + 18, py + 34, "Add to the quick menu", 20, weight="bold"))
    body.append(text(px + pw - 26, py + 30, "x", 18, fill=T["textMuted"]))
    # search
    sy = py + 52
    body.append(f'<rect x="{px + 18}" y="{sy}" width="{pw - 36}" height="30" fill="#0F1214" stroke="{T["accent"]}" stroke-width="1.5"/>')
    body.append(f'<circle cx="{px + 34}" cy="{sy + 14}" r="6" fill="none" stroke="#DDD" stroke-width="2"/><path d="M{px + 38} {sy + 18} l6 6" stroke="#DDD" stroke-width="2"/>')
    body.append(text(px + 52, sy + 20, "chair|", 13))
    body.append(text(px + pw - 34, sy + 20, "x", 13, fill=T["textMuted"]))
    body.append(text(px + 18, sy + 52, "New pins go in front of: Bed   ·   Pinned 7 of 18", 12, fill=T["textMuted"]))
    ly = sy + 70

    def row(y, icon, label, sub, pinned=False, dropdown=False, locked=False, indent=0, expanded=None, hover=False, reason=None):
        out = []
        if hover:
            out.append(f'<rect x="{px + 12}" y="{y}" width="{pw - 24}" height="40" fill="#FFFFFF" fill-opacity="0.07"/>')
        x0 = px + 18 + indent
        if expanded is not None:
            tri = (f"M{x0} {y + 15} l10 0 l-5 8 Z" if expanded else f"M{x0 + 2} {y + 13} l0 12 l8 -6 Z")
            out.append(f'<path d="{tri}" fill="{T["textMuted"]}"/>')
        x0 += 18
        out.append(f'<rect x="{x0}" y="{y + 4}" width="32" height="32" rx="3" fill="{T["slotBg"]}"/>')
        out.append(glyph(icon, x0 + 16, y + 20, 20, grey=locked))
        if dropdown:
            out.append(f'<path d="M{x0 + 24} {y + 4} H{x0 + 32} V{y + 12} Z" fill="#C8CDD2"/>')
        out.append(text(x0 + 42, y + 18, label, 13, fill=T["textMuted"] if locked else T["text"]))
        out.append(text(x0 + 42, y + 34, sub, 11, fill=T["danger"] if reason else T["textMuted"]))
        if pinned:
            out.append(f'<rect x="{px + pw - 92}" y="{y + 10}" width="70" height="20" rx="10" fill="{T["accent"]}" fill-opacity=".18" stroke="{T["accent"]}"/>'
                       + text(px + pw - 57, y + 24, "Pinned", 11, fill=T["accent"], anchor="middle"))
        return "".join(out)

    body.append(text(px + 18, ly + 14, "FURNITURE", 11, fill=T["textMuted"], weight="bold")); ly += 22
    body.append(row(ly, "chair", "Chairs (6 items)", "Furniture · Vanilla Furniture Expanded", pinned=True, dropdown=True, expanded=True)); ly += 44
    body.append(row(ly, "chair", "Armchair", "Furniture · Vanilla Furniture Expanded", indent=24, hover=True)); ly += 44
    body.append(row(ly, "chair", "Dining chair", "Furniture · Core", indent=24)); ly += 44
    body.append(row(ly, "chair", "Stool", "Furniture · Core", indent=24, pinned=True)); ly += 44
    body.append(row(ly, "chair", "Gaming chair", "Furniture · Core", expanded=None)); ly += 44
    body.append(text(px + 18, ly + 14, "IDEOLOGY", 11, fill=T["textMuted"], weight="bold")); ly += 22
    body.append(row(ly, "chair", "Throne chair", "Needs research: Complex furniture", locked=True, reason=True)); ly += 44
    body.append(text(px + 18, ly + 14, "DEV MODE ONLY", 11, fill=T["textMuted"], weight="bold")); ly += 22
    body.append(row(ly, "chair", "Debug chair spawner", "Shown only in dev mode")); ly += 44
    # footer
    body.append(f'<line x1="{px + 12}" y1="{py + ph - 54}" x2="{px + pw - 12}" y2="{py + ph - 54}" stroke="{T["windowBorder"]}"/>')
    body.append(text(px + 18, py + ph - 24, "Click to pin or unpin. The list stays open.", 12, fill=T["textMuted"]))
    body.append(vbutton(px + pw - 138, py + ph - 42, 120, 30, "Close"))
    body.append(text(30, 800, "Settings window (left) and picker window (right). Vanilla Window chrome, buttons, radio buttons and slider; accent only for selection and focus.", 13, fill=T["textMuted"]))
    write("mockup-settings-picker", svg(W, H, "".join(body)))


if __name__ == "__main__":
    mock_wheel()
    mock_sub_and_grid()
    mock_settings_picker()
