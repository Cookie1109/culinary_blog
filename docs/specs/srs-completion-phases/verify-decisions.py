"""Validate decision documentation, coverage and preserved baseline references.

Standard library only. This does not approve proposals or test runtime behavior.
"""

from collections import Counter
from decimal import Decimal
import hashlib
from pathlib import Path
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
register = (HERE / 'decision-register.md').read_text(encoding='utf-8')
inventory = (HERE / 'acceptance-inventory.md').read_text(encoding='utf-8')
errors = []

gaps = re.findall(r'^\| (GAP-\d{3}) /', register, re.M)
expected_gaps = {f'GAP-{i:03}' for i in range(1, 34)}
if set(gaps) != expected_gaps or len(gaps) != 33:
    errors.append('GAP review must cover each GAP-001 through GAP-033 once')

blocks = re.findall(r'^### (DEC-\d{2})[^\n]*\n(.*?)(?=^## |^### |\Z)',
                    register, re.M | re.S)
if {code for code, _ in blocks} != {f'DEC-{i:02}' for i in range(1, 26)} or len(blocks) != 25:
    errors.append('Expected 25 unique decision sections')
pending = set()
for code, block in blocks:
    for field in ('Task/acceptance/nguồn:', 'Trạng thái:'):
        if field not in block:
            errors.append(f'{code}: missing {field}')
    if 'kiểm tiếp' not in block.lower():
        errors.append(f'{code}: missing verification handoff')
    status = re.search(r'^- \*\*Trạng thái:\*\* (.+)', block, re.M)
    if status and 'Đề xuất — chờ duyệt' in status[1]:
        pending.add(code)

expected_pending = {'DEC-02', 'DEC-07', 'DEC-09', 'DEC-11', 'DEC-13', 'DEC-15',
                    'DEC-18', 'DEC-20', 'DEC-21', 'DEC-22', 'DEC-25'}
if pending != expected_pending:
    errors.append(f'Approval status changed; review approval evidence: {sorted(pending)}')
mapped = re.findall(r'^\| (INV-G\d{2}) \| (DEC-\d{2}) \|', register, re.M)
if {code for code, _ in mapped} != {f'INV-G{i:02}' for i in range(1, 13)} or len(mapped) != 12:
    errors.append('Expected complete mapping of 12 inventory handoff groups')
for code, _ in mapped:
    if f'| {code} |' not in inventory:
        errors.append(f'Unknown inventory handoff {code}')

ids = set(re.findall(r'^\| ([A-Z][A-Z0-9/-]+) \|', inventory, re.M))
for ref in re.findall(r'\b((?:FR|NFR)-[A-Z]+-\d{3}/A\d{2}|CT-[A-Z]+(?:-[A-Z]+)?-\d{2})\b', register):
    if ref not in ids:
        errors.append(f'Unknown acceptance/contract reference: {ref}')

baseline_hashes = {
    '00_SRS_Conformance_and_Resolution.md': '714971c09fd73304e9d2b31b663451d5ef9f65e9eb04aad161989c5579a9f3a2',
    '02_Decision_Log.md': '4e68856688080645ab7f18802106dbd6f6ab923209f42f6a4428f46b9ecd574a',
    '09_Test_Strategy.md': 'fa57621c17f18d213044d4443de19289f30da91faf21b9fced34db8bdb4bdba2',
}
for name, digest in baseline_hashes.items():
    if hashlib.sha256((HERE.parent / 'phase-0' / name).read_bytes()).hexdigest() != digest:
        errors.append(f'Baseline changed since review: {name}; check effective approvals')

assert Decimal(365 * 24) * Decimal('0.005') == Decimal('43.800')
assert Decimal(366 * 24) * Decimal('0.005') == Decimal('43.920')
assert Decimal(30 * 24 * 60) * Decimal('0.005') == Decimal('216.000')
assert (Decimal('3.65') / Decimal(365 * 24) * 100).quantize(Decimal('.000001')) == Decimal('.041667')

links = 0
for doc in (HERE / 'decision-register.md', HERE / 'reports/phase-02-result.md'):
    if not doc.exists():
        errors.append(f'Missing {doc.name}')
        continue
    text = doc.read_text(encoding='utf-8')
    columns = None
    for line_number, line in enumerate(text.splitlines(), 1):
        if line.startswith('|'):
            width = len(re.split(r'(?<!\\)\|', line))
            if columns is not None and width != columns:
                errors.append(f'{doc.name}:{line_number}: table column mismatch')
            columns = width
        else:
            columns = None
    for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', text):
        if target.startswith(('http:', 'https:', '#')):
            continue
        links += 1
        if not (doc.parent / target.split('#', 1)[0]).resolve().exists():
            errors.append(f'{doc.name}: missing local link {target}')

for code, count in Counter(gaps + [code for code, _ in blocks]).items():
    if count != 1:
        errors.append(f'Duplicate {code}')
for error in errors:
    print(f'FAIL: {error}')
print(f'GAP reviews={len(gaps)}; decision sections={len(blocks)}; INV mappings={len(mapped)}')
print(f'Pending proposals={len(pending)}; local links checked={links}; unchanged baseline hashes=3')
print('SLA arithmetic: 43.8h/365d, 43.92h/366d, 216min/30d; backup baseline 03:00 UTC')
print('PASS: decision documentation only; approvals/runtime unverified' if not errors else 'FAIL: decision documentation')
raise SystemExit(bool(errors))
