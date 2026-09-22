import json
import os

with open('backend/global.json', 'r') as f:
    data = json.load(f)
data['sdk']['version'] = '9.0.305'
with open('backend/global.json', 'w') as f:
    json.dump(data, f, indent=2)

with open('backend/Directory.Build.props', 'r') as f:
    content = f.read()
content = content.replace('<TargetFramework>net10.0</TargetFramework>', '<TargetFramework>net9.0</TargetFramework>')
with open('backend/Directory.Build.props', 'w') as f:
    f.write(content)

with open('backend/Directory.Packages.props', 'r') as f:
    content = f.read()

# Replace all 10.0.x with 9.0.0
import re
content = re.sub(r'Version="10\.0\.\d+"', 'Version="9.0.0"', content)
# Fix Mapster specifically if needed, but Mapster 9.0.0 is .NET 9 compatible! Wait, Mapster 10.0.0 is NOT .NET 9 compatible?
# The error was "Mapster (>= 9.0.12) but ended up with Mapster 10.0.0". Wait, no, earlier error was NU1601. I'll just change Mapster to 9.0.0 if it's 10.0.0.
content = content.replace('Mapster" Version="10.0.0"', 'Mapster" Version="9.0.0"')

with open('backend/Directory.Packages.props', 'w') as f:
    f.write(content)
