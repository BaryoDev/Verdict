#!/usr/bin/env python3
"""Render the Verdict README banner to PNG at 2x, light and dark, transparent background."""
import subprocess, sys, tempfile
from pathlib import Path

CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
W, H, SCALE = 566, 158, 2

MARK = '''
<svg viewBox="0 0 128 128" width="128" height="128" aria-hidden="true">
  <defs><linearGradient id="m" gradientUnits="userSpaceOnUse" x1="22" y1="65" x2="96" y2="16">
    <stop offset="0" stop-color="#0B6E7C"/><stop offset="1" stop-color="#7FDDEB"/></linearGradient></defs>
  <rect x="26" y="94" width="76" height="15" rx="7.5" fill="#FFC53D"/>
  <path d="M62.26 40.87 L80.26 76.87 L67.74 83.13 L49.74 47.13 Z" fill="#C89B6C"/>
  <circle cx="74" cy="80" r="7" fill="#C89B6C"/>
  <path d="M39.37 63.73 L89.37 38.73 L78.63 17.27 L28.63 42.27 Z" fill="url(#m)"/>
  <circle cx="34" cy="53" r="12" fill="url(#m)"/><circle cx="84" cy="28" r="12" fill="url(#m)"/>
</svg>'''

def page(ink, soft):
    return f'''<!doctype html><meta charset=utf8>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@600&family=IBM+Plex+Mono:wght@400&display=swap">
<style>
  html,body{{margin:0;padding:0;background:transparent}}
  .b{{width:{W}px;height:{H}px;display:flex;align-items:center;gap:18px;padding:0 16px;box-sizing:border-box}}
  .t h1{{font-family:"IBM Plex Sans",system-ui,sans-serif;font-weight:600;font-size:60px;
    line-height:1;margin:0 0 12px;color:{ink};letter-spacing:-.02em}}
  .t p{{font-family:"IBM Plex Mono",monospace;font-size:17px;margin:0;color:{soft};letter-spacing:.01em}}
</style>
<div class="b">{MARK}<div class="t"><h1>Verdict</h1><p>zero-allocation Result types for .NET</p></div></div>'''

def render(html, out):
    with tempfile.TemporaryDirectory() as tmp:
        p = Path(tmp) / "b.html"
        p.write_text(html)
        subprocess.run([CHROME, "--headless", "--disable-gpu", "--hide-scrollbars",
                        "--default-background-color=00000000",
                        f"--force-device-scale-factor={SCALE}",
                        f"--window-size={W},{H}",
                        f"--screenshot={out}", p.as_uri()],
                       check=True, capture_output=True)
    print("wrote", out)

out = Path(sys.argv[1])
render(page("#14171B", "#4C545D"), out / "banner-light.png")
render(page("#E6EAF0", "#9AA6B2"), out / "banner-dark.png")
