"""Save supplied REST credentials via masked stdin into an ignored private file."""
from pathlib import Path
import getpass,re,sys

target=Path(sys.argv[1]).resolve()
host=sys.argv[2]
if not re.fullmatch(r'[a-z0-9-]+\.upstash\.io',host):raise SystemExit('Invalid Upstash hostname.')
if len(sys.argv)==4 and sys.argv[3]=='--update-password':
    values=dict(line.split('=',1) for line in target.read_text(encoding='utf8').splitlines() if line and not line.startswith('#'))
    password=getpass.getpass('Supplied TCP credential (hidden): ')
    if not password or re.search(r'[\r\n\x00]',password):raise SystemExit('Invalid credential; details withheld.')
    values['UPSTASH_REDIS_PASSWORD']=password
    replacement=target.with_suffix('.env.new')
    with replacement.open('x',encoding='utf8',newline='\n') as f:
        for key,value in values.items():f.write(f'{key}={value}\n')
    replacement.replace(target)
    print('Supplied credential saved privately; authentication not yet confirmed.')
    raise SystemExit()
if target.exists():raise SystemExit('Private file exists; not overwritten.')
token=getpass.getpass('Upstash REST token (hidden): ')
if not token or re.search(r'[\s\x00]',token):raise SystemExit('Invalid token; details withheld.')
with target.open('x',encoding='utf8',newline='\n') as f:
    f.write(f'UPSTASH_REDIS_REST_URL=https://{host}\nUPSTASH_REDIS_REST_TOKEN={token}\nUPSTASH_REDIS_PASSWORD=\n')
print('REST credential saved privately. TCP password is still empty; no deployment performed.')
