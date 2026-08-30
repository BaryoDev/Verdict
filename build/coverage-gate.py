#!/usr/bin/env python3
"""Merge every Cobertura report and fail if line coverage is below a threshold.

Each test project writes its own report, and every report lists every package,
so a naive sum of lines-covered over lines-valid counts the same line many times
and lands nowhere near the truth. This takes the union of covered line numbers
per source file instead, which is the only number that means anything when the
suite is split across nine projects.

Usage: coverage-gate.py <results-dir> <minimum-percent>
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict


def measure(paths):
    hit = defaultdict(set)
    seen = defaultdict(set)
    for path in paths:
        root = ET.parse(path).getroot()
        for package in root.iter("package"):
            for cls in package.iter("class"):
                key = (package.get("name"), cls.get("filename"))
                for line in cls.findall("./lines/line"):
                    number = int(line.get("number"))
                    seen[key].add(number)
                    if int(line.get("hits", 0)) > 0:
                        hit[key].add(number)
    return hit, seen


def main():
    if len(sys.argv) != 3:
        print(__doc__.strip())
        return 2

    results_dir, minimum = sys.argv[1], float(sys.argv[2])
    reports = glob.glob(os.path.join(results_dir, "**", "coverage.cobertura.xml"), recursive=True)
    if not reports:
        print(f"::error::No coverage reports under {results_dir}. The collector did not run.")
        return 1

    hit, seen = measure(reports)

    by_package = defaultdict(lambda: [0, 0])
    for key, lines in seen.items():
        by_package[key[0]][0] += len(hit[key])
        by_package[key[0]][1] += len(lines)

    covered = sum(v[0] for v in by_package.values())
    total = sum(v[1] for v in by_package.values())
    percent = 100.0 * covered / total if total else 0.0

    print(f"Merged {len(reports)} report(s).")
    for package, (c, t) in sorted(by_package.items(), key=lambda kv: kv[1][0] / max(1, kv[1][1])):
        print(f"  {package:24} {c:5}/{t:<5} {100.0 * c / t:6.2f}%")
    print(f"  {'OVERALL':24} {covered:5}/{total:<5} {percent:6.2f}%")

    if percent + 1e-9 < minimum:
        print(f"::error::Line coverage {percent:.2f}% is below the {minimum:.2f}% floor.")
        return 1

    print(f"Coverage {percent:.2f}% meets the {minimum:.2f}% floor.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
