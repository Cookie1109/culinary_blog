"""Check documentation coverage and references; does not certify SRS behavior.

Run from any cwd with Python 3 (standard library only).
"""

from collections import Counter
import hashlib
from pathlib import Path
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
inventory = (HERE / 'acceptance-inventory.md').read_text(encoding='utf-8')

expected = {
    *(f'FR-{group}-{number:03}' for group, count in {
        'AUTH': 7, 'CAT': 5, 'RCP': 10, 'SRCH': 4,
        'FILE': 2, 'JOB': 3, 'OBS': 3,
    }.items() for number in range(1, count + 1)),
    *(f'NFR-{group}-{number:03}' for group, count in {
        'PERF': 5, 'SEC': 7, 'USE': 4, 'REL': 3,
        'MAINT': 4, 'SCALE': 3, 'SEO': 4,
    }.items() for number in range(1, count + 1)),
    *(f'CONS-{number:03}' for number in range(1, 11)),
}
errors = []
columns = None
for number, line in enumerate(inventory.splitlines(), 1):
    if line.startswith('|'):
        width = len(re.split(r'(?<!\\)\|', line))
        if columns is not None and width != columns:
            errors.append(f'Markdown table column mismatch at line {number}')
        columns = width
    else:
        columns = None
blocks = re.findall(r'^### ((?:FR-|NFR-|CONS-)\S+)\n(.*?)(?=^###? |\Z)',
                    inventory, re.M | re.S)
codes = [code for code, _ in blocks]
if set(codes) != expected or len(codes) != len(expected):
    errors.append(f'Requirements missing={expected - set(codes)}, extra/duplicates={set(codes) - expected}')

acceptances = []
for code, block in blocks:
    for label in ('Nguồn: SRS tr.', 'Ưu tiên:', 'Đối tượng:',
                  'Phase tiếp nhận:', 'Hiện trạng/evidence:', 'Quyết định/điểm mở:'):
        if label not in block:
            errors.append(f'{code}: missing {label}')
    rows = re.findall(r'^\| (' + re.escape(code) + r'/A\d+) \| (.+)$', block, re.M)
    if len(rows) < 2:
        errors.append(f'{code}: expected decomposed acceptances')
    for row_id, content in rows:
        cells = re.split(r'(?<!\\)\|', content)
        if len(cells) != 4 or not all(cell.strip() for cell in cells[:3]):
            errors.append(f'{row_id}: malformed/empty table cells')
        if 'Chưa kiểm runtime' not in content:
            errors.append(f'{row_id}: unreviewed change of evidence status')
        acceptances.append(row_id)

contracts = re.findall(r'^\| (CT-[A-Z]+(?:-[A-Z]+)?-\d+) \|', inventory, re.M)
for prefix in ('UI', 'HTTP', 'EXT', 'HW', 'ARCH', 'DATA', 'API', 'SUP',
               'QUERY', 'SCHEMA', 'ERR', 'GLOSS'):
    if not any(row.startswith(f'CT-{prefix}-') for row in contracts):
        errors.append(f'Missing contract section {prefix}')
for row_id, count in Counter(acceptances + contracts).items():
    if count != 1:
        errors.append(f'Duplicate ID {row_id}')

# Every OpenAPI operation must have an explicit API/supporting inventory row.
# The existing YAML uses two/four-space indentation for paths/methods.
spec = (ROOT / 'docs/specs/phase-0/openapi.v1.yaml').read_text(encoding='utf-8')
operations = set()
path = None
for line in spec.splitlines():
    match = re.fullmatch(r'  (/[^:]+):', line)
    if match:
        path = match[1]
    method = re.fullmatch(r'    (get|post|put|patch|delete):', line)
    if method and path:
        operations.add((method[1].upper(), re.sub(r'\{[^}]+\}', '{}', path)))
documented = {
    (method, re.sub(r'\{[^}]+\}', '{}', path))
    for method, path in re.findall(
        r'^\| CT-(?:API|SUP)-\d+ \| [^|]*?\b(GET|POST|PUT|PATCH|DELETE) (/[^\s|]+)',
        inventory, re.M)
}
if operations - documented:
    errors.append(f'OpenAPI operations missing inventory: {sorted(operations - documented)}')

link_count = 0
for doc in (HERE / 'acceptance-inventory.md', HERE / 'reports/phase-01-result.md'):
    if not doc.exists():
        errors.append(f'Missing document {doc.name}')
        continue
    for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', doc.read_text(encoding='utf-8')):
        if target.startswith(('https:', 'http:', '#')):
            continue
        link_count += 1
        if not (doc.parent / target.split('#', 1)[0]).resolve().exists():
            errors.append(f'{doc.name}: broken local link {target}')

pdf = ROOT / 'docs/specs/SRS_Culinary_Blog_v1.0.0.pdf'
digest = hashlib.sha256(pdf.read_bytes()).hexdigest()
if digest != '8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063':
    errors.append('SRS source changed: re-review page references and acceptances')

for error in errors:
    print(f'FAIL: {error}')
print(f'Requirements: FR={sum(c.startswith("FR-") for c in codes)}, '
      f'NFR={sum(c.startswith("NFR-") for c in codes)}, '
      f'CONS={sum(c.startswith("CONS-") for c in codes)}')
print(f'Acceptances={len(acceptances)}; contracts={len(contracts)}; '
      f'OpenAPI operations={len(operations)}; local links checked={link_count}')
print(f'SRS SHA256={digest}')
print('PASS: documentation structure and references only' if not errors else 'FAIL: documentation checks')
raise SystemExit(bool(errors))
