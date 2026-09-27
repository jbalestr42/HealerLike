#!/usr/bin/env python3
"""Compare capture folders against frozen controls and retain an auditable record."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops


def frames(path):
    return sorted(path.glob("*.png")) if path and path.is_dir() else []


def compare(frame, control, diff_dir):
    image = Image.open(frame).convert("RGBA")
    reference = Image.open(control).convert("RGBA")
    if image.size != reference.size:
        return {"frame": str(frame), "control": str(control), "different_pixels": -1,
                "bbox": None, "reason": "size differs"}
    diff = ImageChops.difference(image, reference)
    bbox = diff.getbbox()
    pixels = 0
    if bbox:
        pixels = sum(1 for pixel in diff.getdata() if pixel != (0, 0, 0, 0))
        diff_dir.mkdir(parents=True, exist_ok=True)
        diff.save(diff_dir / frame.name)
    return {"frame": str(frame), "control": str(control), "different_pixels": pixels,
            "bbox": list(bbox) if bbox else None}


def group(name, current, control, output):
    result = {"name": name, "current_path": str(current) if current else None,
              "control_path": str(control) if control else None, "frames": []}
    if not current or not current.is_dir():
        result["status"] = "absent"
        return result
    if not control or not control.is_dir():
        result["status"] = "control absent"
        result["frames"] = [{"frame": str(frame), "control": None,
                             "different_pixels": None, "bbox": None,
                             "reason": "frozen control absent"} for frame in frames(current)]
        return result
    result["status"] = "compared"
    diff_dir = output.parent / (output.stem + "-diffs") / name
    controls = {frame.name: frame for frame in frames(control)}
    for frame in frames(current):
        reference = controls.get(frame.name)
        if reference:
            result["frames"].append(compare(frame, reference, diff_dir))
        else:
            result["frames"].append({"frame": str(frame), "control": None,
                                     "different_pixels": None, "bbox": None,
                                     "reason": "matching control frame absent"})
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="comparison.json", type=Path)
    parser.add_argument("--portrait-root", type=Path,
                        default=Path("/tmp/healerlike-portrait-backplate/evidence"))
    parser.add_argument("--spell-root", type=Path,
                        default=Path("/tmp/healerlike-spell-sources/evidence"))
    args = parser.parse_args()
    portrait = args.portrait_root
    spell = args.spell_root
    groups = [
        group("complete-forest", portrait / "complete-forest", portrait / "baseline-forest", args.output),
        group("complete-moon", portrait / "complete-moon", portrait / "baseline-moon", args.output),
        group("spell-sources", spell / "spell-sources", spell / "baseline-spell-sources", args.output),
    ]
    args.output.write_text(json.dumps({"groups": groups}, indent=2) + "\n")
    print(json.dumps({g["name"]: {"status": g["status"], "frames": len(g["frames"]),
                                  "nonzero": sum(1 for f in g["frames"]
                                                  if (f.get("different_pixels") or 0) > 0)}
                     for g in groups}, indent=2))


if __name__ == "__main__":
    main()
