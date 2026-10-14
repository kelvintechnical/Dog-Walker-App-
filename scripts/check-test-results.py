"""CI must not treat zero tests, failures, or skipped SQL coverage as success."""
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
counters = root.find("t:ResultSummary/t:Counters", ns)
if counters is None:
    raise SystemExit("Missing test counters")
values = {k: int(v) for k, v in counters.attrib.items()}
if values.get("total", 0) == 0 or values.get("passed") != values.get("total"):
    raise SystemExit("Required CI suite did not pass every test: " + str(values))
print("All required tests passed:", values["passed"])
