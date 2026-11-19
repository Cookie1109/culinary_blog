"""Prepare Redis-only Render import and update existing private deployment drafts."""
from pathlib import Path
from datetime import datetime,timezone
import argparse,hashlib,json,re,subprocess,urllib.parse

p=argparse.ArgumentParser();p.add_argument('--env-file',required=True,type=Path);a=p.parse_args()
private=a.env_file.resolve().parent;repo=Path(__file__).resolve().parents[2]
values=dict(line.split('=',1) for line in a.env_file.read_text(encoding='utf8').splitlines() if line and not line.startswith('#'))
tested=json.loads((private/'tcp-check.json').read_text(encoding='utf8'))
if not tested['passed'] or not tested.get('ownNamespaceCleaned'):raise SystemExit('Successful TCP compatibility check and cleanup required.')
configuration=(private/'render-redis-connection.txt').read_text(encoding='utf8').strip()
host=urllib.parse.urlsplit(values['UPSTASH_REDIS_REST_URL']).hostname
if not host or host not in configuration or values['UPSTASH_REDIS_PASSWORD'] not in configuration or '\n' in configuration or '\r' in configuration:
    raise SystemExit('Prepared connection does not match private inputs.')
if any(c in configuration for c in ['#','"',"'"]):raise SystemExit('Review dotenv quoting before importing this credential.')
import_file=private/'render-redis.env'
if subprocess.run(['git','check-ignore','--quiet',str(import_file)],cwd=repo).returncode:
    raise SystemExit('Private import must be Git-ignored.')
import_file.write_text('ConnectionStrings__Redis='+configuration+'\n',encoding='utf8',newline='\n')
pattern=rb'(?m)^ConnectionStrings__Redis=[^\r\n]*'
updated=[]
for relative in ['backend/.env','tmp/render/backend-render.env.draft']:
    target=repo/relative
    if not target.exists():continue
    if subprocess.run(['git','check-ignore','--quiet',str(target)],cwd=repo).returncode:
        raise SystemExit('Private deployment draft is not ignored; stop.')
    original=target.read_bytes()
    if len(re.findall(pattern,original))!=1:raise SystemExit('Expected one Redis setting in private deployment draft.')
    replacement=re.sub(pattern,lambda _:b'ConnectionStrings__Redis='+configuration.encode('utf8'),original)
    if re.sub(pattern,b'',original)!=re.sub(pattern,b'',replacement):raise SystemExit('Non-Redis changes detected; stop.')
    temporary=target.with_name(target.name+'.upstash.tmp')
    with temporary.open('xb') as f:f.write(replacement)
    temporary.replace(target)
    updated.append({'file':relative,'onlyRedisVariableChanged':True})
report={'preparedAtUtc':datetime.now(timezone.utc).isoformat(),'endpointSha256':hashlib.sha256(host.encode()).hexdigest(),'environmentVariable':'ConnectionStrings__Redis','importFile':str(import_file),'updatedPrivateDrafts':updated,'renderConfigurationApplied':False,'productionDeployed':False,'localComposeConfigurationChanged':False,'postgresMinioSettingsChanged':False}
(private/'render-preparation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
print(json.dumps(report))
