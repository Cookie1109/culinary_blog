"""Prepare a private Neon/Upstash env patch; never apply it to Render or local Compose."""
from pathlib import Path
from datetime import datetime,timezone
import argparse,json,re,subprocess,urllib.parse

p=argparse.ArgumentParser();p.add_argument('--neon-directory',required=True,type=Path);p.add_argument('--redis-directory',required=True,type=Path);p.add_argument('--render-url',required=True);a=p.parse_args()
repo=Path(__file__).resolve().parents[2];neon=a.neon_directory.resolve();redis=a.redis_directory.resolve()
verification=json.loads((neon/'neon-restore-verification.json').read_text(encoding='utf8'))
npgsql=json.loads((neon/'render-npgsql-review.json').read_text(encoding='utf8'))
ping=json.loads((redis/'tcp-ping.json').read_text(encoding='utf8'))
if not verification['passed'] or not npgsql.get('npgsqlConnected') or not ping['passed']:
    raise SystemExit('Verified data and both live client connections are required.')
database=(neon/'render-neon-connection.txt').read_text(encoding='utf8').strip()
cache=(redis/'render-redis-connection.txt').read_text(encoding='utf8').strip()
render_url=urllib.parse.urlsplit(a.render_url)
if render_url.scheme!='https' or not render_url.hostname or render_url.username or render_url.path not in ('','/'):
    raise SystemExit('Expected supplied Render HTTPS origin.')
patch={'ASPNETCORE_ENVIRONMENT':'Production','ASPNETCORE_HTTP_PORTS':'8080','AllowedHosts':render_url.hostname,'ConnectionStrings__Database':database,'ConnectionStrings__Redis':cache,'Database__ApplyMigrationsOnStartup':'false','AdminSeed__Enabled':'false','BackgroundJobs__Enabled':'false','Release__Provider':'Render'}
if any(any(c in value for c in '\r\n\x00#\'"') for value in patch.values()):
    raise SystemExit('Review dotenv quoting for supplied values before generating import.')
import_file=redis/'render-neon-upstash.env'
if subprocess.run(['git','check-ignore','--quiet',str(import_file)],cwd=repo).returncode:
    raise SystemExit('Private import must be Git ignored.')
import_file.write_text(''.join(key+'='+value+'\n' for key,value in patch.items()),encoding='utf8',newline='\n')
updates=[];configuration_reports=[]
for relative in ['backend/.env','tmp/render/backend-render.env.draft']:
    target=repo/relative
    if not target.exists():continue
    if subprocess.run(['git','check-ignore','--quiet',str(target)],cwd=repo).returncode:raise SystemExit('Private draft must be ignored.')
    original=target.read_bytes();updated=original
    patterns=[]
    for key,value in patch.items():
        pattern=rb'(?m)^'+re.escape(key.encode())+rb'=[^\r\n]*(?:\r?\n|$)';patterns.append(pattern)
        count=len(re.findall(pattern,original))
        if count==0 and key=='Release__Provider':
            if not updated.endswith(b'\n'):raise SystemExit('Private draft must end with a newline before adding metadata.')
            updated+=key.encode()+b'='+value.encode()+b'\n'
        elif count==1:
            updated=re.sub(pattern,lambda m,key=key,value=value:key.encode()+b'='+value.encode()+(b'\r\n' if m[0].endswith(b'\r\n') else b'\n' if m[0].endswith(b'\n') else b''),updated)
        else:raise SystemExit('Expected exactly one existing variable per patch key.')
    before=original;after=updated
    for pattern in patterns:before=re.sub(pattern,b'',before);after=re.sub(pattern,b'',after)
    if before!=after:raise SystemExit('Unexpected unrelated configuration mutation.')
    temporary=target.with_name(target.name+'.connections.tmp')
    with temporary.open('xb') as f:f.write(updated)
    temporary.replace(target)
    updates.append({'file':relative,'onlyConnectionAndSafetyVariablesTouched':True,'otherBytesUnchanged':True})
    values=dict(line.split('=',1) for line in updated.decode('utf-8-sig').splitlines() if line and not line.startswith('#') and '=' in line)
    placeholders=['local-development-only','culinary-local','change-this','replace-with']
    missing=[]
    for key in ['ConnectionStrings__Database','Jwt__SigningKey','ObjectStorage__AccessKey','ObjectStorage__SecretKey']:
        value=values.get(key,'')
        if not value or any(marker in value.lower() for marker in placeholders):missing.append(key)
    if values.get('Release__Provider')!='Render':
        if not re.fullmatch(r'[0-9a-f]{40}',values.get('Release__CommitSha','')):missing.append('Release__CommitSha')
        for key in ['Release__ApiImageDigest','Release__WebImageDigest']:
            if not re.fullmatch(r'sha256:[0-9a-f]{64}',values.get(key,'')):missing.append(key)
    elif values.get('Release__CommitSha') and not re.fullmatch(r'[0-9a-f]{40}',values['Release__CommitSha']):missing.append('Release__CommitSha')
    if not values.get('AllowedHosts') or values['AllowedHosts']=='*':missing.append('AllowedHosts')
    for key in ['Sitemap__PublicBaseUrl']+[k for k in values if k.startswith('Cors__AllowedOrigins__')]:
        uri=urllib.parse.urlsplit(values.get(key,''))
        if uri.scheme!='https' or not uri.netloc:missing.append(key)
    configuration_reports.append({'file':relative,'productionGuardMissingOrInvalidKeys':missing,'portSetting':values.get('ASPNETCORE_HTTP_PORTS'),'postgresAndRedisVariablesFilled':True,'databaseStartupMigrations':values.get('Database__ApplyMigrationsOnStartup'),'adminSeed':values.get('AdminSeed__Enabled'),'backgroundJobs':values.get('BackgroundJobs__Enabled')})
report={'preparedAtUtc':datetime.now(timezone.utc).isoformat(),'importFile':str(import_file),'variables':list(patch),'privateDrafts':updates,'configurationReports':configuration_reports,'releaseProvider':'Render','runtimeMetadataRequiredFromRender':['RENDER','RENDER_GIT_COMMIT','RENDER_SERVICE_ID','RENDER_INSTANCE_ID'],'runtimeMetadataVerifiedOnRender':False,'currentWorkingTreeSchemaReady':npgsql['currentWorkingTreeSchemaReady'],'renderUpdated':False,'localDevelopmentOrMinioChanged':False,'databaseWritesPerformed':False}
(redis/'combined-render-preparation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
print(json.dumps(report))
