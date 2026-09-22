import json

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
content = content.replace('Version="10.', 'Version="9.')
content = content.replace('Mapster" Version="9.0.0"', 'Mapster" Version="7.4.0"')
with open('backend/Directory.Packages.props', 'w') as f:
    f.write(content)
