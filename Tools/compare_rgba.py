#!/usr/bin/env python3
"""Compare two image folders by filename and count changed RGBA pixels."""
from pathlib import Path
import sys
from PIL import Image

before, after = map(Path, sys.argv[1:3])
left = {p.name: p for p in before.glob("*.png")}
right = {p.name: p for p in after.glob("*.png")}
for name in sorted(left.keys() | right.keys()):
    if name not in left or name not in right:
        print(f"{name}\tmissing")
        continue
    a = Image.open(left[name]).convert("RGBA")
    b = Image.open(right[name]).convert("RGBA")
    if a.size != b.size:
        print(f"{name}\tsize {a.size}!={b.size}")
        continue
    changed = sum(x != y for x, y in zip(a.getdata(), b.getdata()))
    print(f"{name}\t{changed}")
