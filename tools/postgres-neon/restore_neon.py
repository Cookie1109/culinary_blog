"""One approved archive restore; never clean/drop, switch Render, migrate or seed."""
from pathlib import Path
from datetime import datetime,timezone
import argparse,hashlib,json,os,ssl,subprocess,sys

p=argparse.ArgumentParser()
p.add_argument('--directory',required=True,type=Path)
p.add_argument('--bin-directory',required=True,type=Path)
a=p.parse_args();out=a.directory.resolve();private=out/'neon-private'
repo=Path(__file__).resolve().parents[2]
if out==repo or repo in out.parents:raise SystemExit('Private archive directory must be outside Git repository.')
approval=json.loads((out/'restore-approval.json').read_text())
plan=json.loads((out/'restore-plan-awaiting-approval.json').read_text())
archive=out/'culinary-local.dump';journal=out/'neon-restore-execution.json'
if journal.exists():raise SystemExit('Restore journal already exists. Review it; automatic retry is forbidden.')
if not approval.get('approved') or approval['archiveSha256']!=plan['sourceArchiveSha256'] or approval['endpointHostSha256']!=plan['endpointHostSha256']:
    raise SystemExit('Exact archive/endpoint approval missing.')
with archive.open('rb') as f:
    if hashlib.file_digest(f,'sha256').hexdigest()!=approval['archiveSha256']:raise SystemExit('Approved archive hash mismatch.')
conf=dict(line.split('=',1) for line in (private/'pg_service.conf').read_text().splitlines() if '=' in line)
if hashlib.sha256(conf['host'].encode()).hexdigest()!=approval['endpointHostSha256'] or conf['dbname']!='neondb' or '-pooler.' in conf['host']:
    raise SystemExit('Approved Direct endpoint/database mismatch.')
if conf.get('sslmode')!='verify-full' or conf.get('channel_binding')!='require':raise SystemExit('TLS configuration changed.')
ca=private/'isrg-root-x1.pem'
if hashlib.sha256(ssl.PEM_cert_to_DER_cert(ca.read_text())).hexdigest()!='96bcec06264976f37460779acf28c5a7cfe8a3c0aae11a8ffcee05c0bddf08c6':
    raise SystemExit('Scoped client CA mismatch.')
psql=a.bin_directory/'psql.exe';restore=a.bin_directory/'pg_restore.exe'
for executable in [psql,restore]:
    version=subprocess.run([str(executable),'--version'],capture_output=True,text=True,check=True).stdout
    if '(PostgreSQL) 18.' not in version:raise SystemExit('Reviewed PostgreSQL18 client required.')
check=subprocess.run([sys.executable,str(Path(__file__).with_name('inspect_neon.py')),'--directory',str(out),'--psql',str(psql)],capture_output=True,text=True)
if check.returncode:raise SystemExit('Immediate read-only preflight failed; no restore attempted.')
preflight=json.loads((out/'neon-preflight.json').read_text())
if preflight['tables'] or preflight['otherRelations'] or preflight['userFunctions']:
    raise SystemExit('Target no longer empty. Stop and review a new plan, no restore attempted.')
if preflight['endpointHostSha256']!=approval['endpointHostSha256'] or preflight['server']['database']!='neondb':raise SystemExit('Target changed.')
if not preflight['server']['createDatabaseObjects'] or not preflight['server']['publicSchemaCreate']:
    raise SystemExit('Target permissions changed.')
if {s['name'] for s in preflight['schemas']}!={'public'} or {e['name'] for e in preflight['extensions']}!={'plpgsql'}:
    raise SystemExit('Target schema/extensions changed; review before restore.')
if not next(s for s in preflight['schemas'] if s['name']=='public')['ownermembership']:
    raise SystemExit('Public schema owner permission changed.')
available={e['name'] for e in preflight['availableExtensions']}
if not {'plpgsql','unaccent','pg_trgm'}<=available:raise SystemExit('Required extension unavailable.')
environment=os.environ.copy()
for key in ['PGHOST','PGHOSTADDR','PGPORT','PGDATABASE','PGUSER','PGPASSWORD','PGSSLMODE','PGSSLROOTCERT','PGCHANNELBINDING','PGOPTIONS']:
    environment.pop(key,None)
environment.update(PGSERVICE='neon_readonly',PGSERVICEFILE=str(private/'pg_service.conf'),PGPASSFILE=str(private/'pgpass'),SSL_CERT_FILE=str(ca),PGCLIENTENCODING='UTF8',PGAPPNAME='culinary-approved-snapshot-restore',PGOPTIONS='-c default_transaction_read_only=off -c statement_timeout=120000 -c lock_timeout=10000')
record={'startedAtUtc':datetime.now(timezone.utc).isoformat(),'archiveSha256':approval['archiveSha256'],'endpointHostSha256':approval['endpointHostSha256'],'targetDatabase':'neondb','approvalFile':'restore-approval.json','preflightEmpty':True,'flags':['--no-owner','--no-acl','--single-transaction','--exit-on-error'],'status':'restore_started_do_not_retry','restoreCommitted':None,'sourceWritesPerformed':False,'renderSwitchPerformed':False}
with journal.open('x',encoding='utf8') as f:json.dump(record,f,indent=2)
try:
    result=subprocess.run([str(restore),'-w','--no-owner','--no-acl','--single-transaction','--exit-on-error','--dbname=service=neon_readonly',str(archive)],env=environment,capture_output=True,timeout=300)
    error=(result.stdout+result.stderr).decode('utf8',errors='replace')
    secret=(private/'pgpass').read_text().strip().split(':',4)[4].replace('\\:',':').replace('\\\\','\\')
    for value in [conf['host'],conf['user'],secret]:error=error.replace(value,'[redacted]')
    (out/'neon-restore.redacted.log').write_text(error,encoding='utf8')
    record.update(finishedAtUtc=datetime.now(timezone.utc).isoformat(),exitCode=result.returncode,status='restore_committed_verification_pending' if result.returncode==0 else 'restore_failed_stop_review',restoreCommitted=result.returncode==0)
except Exception:
    record.update(status='restore_interrupted_outcome_requires_readonly_review',finishedAtUtc=datetime.now(timezone.utc).isoformat())
    journal.write_text(json.dumps(record,indent=2)+'\n',encoding='utf8')
    raise SystemExit('Restore interrupted; do not retry. Review private journal and target read-only.') from None
journal.write_text(json.dumps(record,indent=2)+'\n',encoding='utf8')
if result.returncode:raise SystemExit('Restore failed in single transaction; stop and review redacted private log. No automatic retry.')
print(json.dumps({'restoreCommitted':True,'verificationPending':True,'database':'neondb','sourceChanged':False,'renderChanged':False}))
