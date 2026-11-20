"""Save a Neon URI received through masked stdin, never command arguments."""
from pathlib import Path
import getpass,json,re,sys,urllib.parse

target=Path(sys.argv[1]).resolve()
repo=Path(__file__).resolve().parents[2]
if target==repo or repo in target.parents: raise SystemExit('Private directory must be outside repository.')
target.mkdir(parents=True,exist_ok=True)
if any((target/p).exists() for p in ['pg_service.conf','pgpass']): raise SystemExit('Private files already exist; not overwritten.')
uri=getpass.getpass('Neon DIRECT URI (hidden): ')
try:
    parsed=urllib.parse.urlsplit(uri)
    host=parsed.hostname or ''
    if parsed.scheme not in ['postgres','postgresql'] or not re.fullmatch(r'ep-[a-z0-9.-]+\.neon\.tech',host) or '-pooler.' in host: raise ValueError()
    database=urllib.parse.unquote(parsed.path.lstrip('/'))
    role=urllib.parse.unquote(parsed.username or '')
    password=urllib.parse.unquote(parsed.password or '')
    if database!='neondb' or not re.fullmatch(r'[a-zA-Z0-9_-]+',role) or not password or re.search(r'[\r\n\x00]',password): raise ValueError()
    port=parsed.port or 5432
    service=f'[neon_readonly]\nhost={host}\nport={port}\ndbname={database}\nuser={role}\nsslmode=verify-full\nsslrootcert=system\nchannel_binding=require\nconnect_timeout=15\napplication_name=culinary_neon_readonly_preflight\n'
    escaped=password.replace('\\','\\\\').replace(':','\\:')
    (target/'pg_service.conf').write_text(service,encoding='utf8',newline='\n')
    (target/'pgpass').write_text(f'{host}:{port}:{database}:{role}:{escaped}\n',encoding='utf8',newline='\n')
    (target/'intent.json').write_text(json.dumps({'database':database,'branchReportedByUser':'production','cloudWritesAuthorized':False},indent=2)+'\n')
except Exception:
    raise SystemExit('Invalid connection input; details withheld.') from None
finally:
    uri=parsed=password=escaped=service=None
print('Private connection saved; no connection or restore performed. Secret input was not echoed.')
