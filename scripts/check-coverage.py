"""Fails when line coverage in the newest Cobertura report is below the threshold (claude.md: at least 80 %)."""
import glob
import os
import sys
import xml.etree.ElementTree as ET

threshold = float(sys.argv[1]) if len(sys.argv) > 1 else 80.0
reports = glob.glob("TestResults/**/coverage.cobertura.xml", recursive=True)
if not reports:
    sys.exit("No coverage report found under TestResults/.")

newest = max(reports, key=os.path.getmtime)
root = ET.parse(newest).getroot()
covered, valid = int(root.get("lines-covered")), int(root.get("lines-valid"))
rate = 100.0 * covered / valid if valid else 0.0
print(f"Report: {newest}")
for package in root.iter("package"):
    print(f"  {package.get('name')}: {100 * float(package.get('line-rate')):.1f} %")
print(f"Line coverage: {rate:.1f} % ({covered}/{valid}), threshold {threshold:.0f} %")
if rate < threshold:
    sys.exit(f"Line coverage {rate:.1f} % is below {threshold:.0f} %.")
