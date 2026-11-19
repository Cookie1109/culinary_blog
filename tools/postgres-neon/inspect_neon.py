"""Read-only Neon preflight. No restore, DDL, seed or connection-string output."""
from pathlib import Path
import argparse,hashlib,json,os,re,ssl,subprocess
from datetime import datetime,timezone

p=argparse.ArgumentParser();p.add_argument('--directory',required=True)
client=p.add_mutually_exclusive_group(required=True)
client.add_argument('--client-image');client.add_argument('--psql',type=Path)
args=p.parse_args()
out=Path(args.directory).resolve();private=out/'neon-private'
for name in ['pg_service.conf','pgpass','intent.json']:
    if not (private/name).is_file():raise SystemExit('Private connection files are not ready.')
conf=dict(line.split('=',1) for line in (private/'pg_service.conf').read_text().splitlines() if '=' in line)
if '-pooler.' in conf['host'] or conf['dbname']!='neondb':raise SystemExit('Direct neondb endpoint required.')
shell='''umask 077
cp /run/neon/pgpass /tmp/neon.pgpass
chmod 600 /tmp/neon.pgpass
export PGPASSFILE=/tmp/neon.pgpass
export PGOPTIONS='-c default_transaction_read_only=on -c statement_timeout=15000 -c lock_timeout=5000'
exec psql -X -w -qAt -v ON_ERROR_STOP=1
'''
command=['docker','run','--rm','-i','--read-only','--tmpfs','/tmp:rw,noexec,nosuid,size=16m','--mount',f'type=bind,source={private},target=/run/neon,readonly','--env','PGSERVICE=neon_readonly','--env','PGSERVICEFILE=/run/neon/pg_service.conf']
if (private/'isrg-root-x1.pem').exists():
    fingerprint=hashlib.sha256(ssl.PEM_cert_to_DER_cert((private/'isrg-root-x1.pem').read_text())).hexdigest()
    if fingerprint!='96bcec06264976f37460779acf28c5a7cfe8a3c0aae11a8ffcee05c0bddf08c6':raise SystemExit('Client CA fingerprint mismatch.')
    command+=['--env','SSL_CERT_FILE=/run/neon/isrg-root-x1.pem']
command+=['--entrypoint','sh',args.client_image,'-ec',shell]
environment=None
if args.psql:
    if not args.psql.is_file():raise SystemExit('Native psql executable not found.')
    environment=os.environ.copy()
    environment.update(PGSERVICE='neon_readonly',PGSERVICEFILE=str(private/'pg_service.conf'),PGPASSFILE=str(private/'pgpass'),PGOPTIONS='-c default_transaction_read_only=on -c statement_timeout=15000 -c lock_timeout=5000',PGCLIENTENCODING='UTF8')
    if (private/'isrg-root-x1.pem').exists():environment['SSL_CERT_FILE']=str(private/'isrg-root-x1.pem')
    command=[str(args.psql),'-X','-w','-qAt','-v','ON_ERROR_STOP=1']
process=subprocess.Popen(command,env=environment,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf8')
def query(sql):
    process.stdin.write(sql+'\n');process.stdin.flush()
    value=process.stdout.readline().strip()
    if not value:raise RuntimeError('Read-only connection/query failed; provider details withheld.')
    return json.loads(value)
def array(sql):return query("SELECT coalesce(json_agg(row_to_json(q)),'[]'::json) FROM ("+sql+') q;')
def ident(name):return '"'+name.replace('"','""')+'"'
try:
    process.stdin.write("BEGIN READ ONLY; SET LOCAL TIME ZONE 'UTC';\n");process.stdin.flush()
    server=query("SELECT json_build_object('database',current_database(),'user',current_user,'version',version(),'versionNum',current_setting('server_version_num'),'readOnly',current_setting('transaction_read_only'),'createDatabaseObjects',has_database_privilege(current_user,current_database(),'CREATE'),'publicSchemaCreate',has_schema_privilege(current_user,'public','CREATE'),'superuser',(SELECT rolsuper FROM pg_roles WHERE rolname=current_user),'tls',(SELECT row_to_json(t) FROM (SELECT ssl,version,cipher,bits FROM pg_stat_ssl WHERE pid=pg_backend_pid()) t));")
    (out/'neon-connection-check.json').write_text(json.dumps(server,indent=2)+'\n')
    server['databaseSettings']=query("SELECT json_build_object('encoding',pg_encoding_to_char(encoding),'collate',datcollate,'ctype',datctype,'provider',datlocprovider,'locale',coalesce(to_jsonb(d)->>'datlocale',to_jsonb(d)->>'daticulocale'),'collationVersion',datcollversion) FROM pg_database d WHERE datname=current_database();")
    process.stdin.write('\\conninfo\n\\echo NEON_CONNINFO_END\n');process.stdin.flush()
    connection=[]
    while True:
        text=process.stdout.readline().strip()
        if text=='NEON_CONNINFO_END':break
        if not text and process.poll() is not None:raise RuntimeError('Client connection check failed.')
        connection.append(text)
    clientTls=bool(re.search(r'TLSv1\.[23]', '\n'.join(connection)))
    if conf.get('sslmode')!='verify-full' or conf.get('channel_binding')!='require' or not clientTls:raise RuntimeError('Client TLS verification not confirmed.')
    if server['database']!='neondb' or server['readOnly']!='on':raise RuntimeError('Wrong database or read-write session.')
    base="n.nspname NOT LIKE 'pg_%' AND n.nspname <> 'information_schema'"
    schemas=array('SELECT n.nspname AS name,pg_get_userbyid(n.nspowner) AS owner,has_schema_privilege(current_user,n.oid,\'CREATE\') AS cancreate,pg_has_role(current_user,n.nspowner,\'USAGE\') AS ownerMembership FROM pg_namespace n WHERE '+base+' ORDER BY 1')
    tables=array('SELECT n.nspname AS schema,c.relname AS name,c.relkind AS kind,pg_get_userbyid(c.relowner) AS owner FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+" AND c.relkind IN ('r','p','f') ORDER BY 1,2")
    for table in tables:
        table['rows']=query('SELECT to_json(count(*)) FROM '+ident(table['schema'])+'.'+ident(table['name'])+';')
    extensions=array('SELECT e.extname AS name,e.extversion AS version,n.nspname AS schema,pg_get_userbyid(e.extowner) AS owner FROM pg_extension e JOIN pg_namespace n ON n.oid=e.extnamespace ORDER BY 1')
    available=array("SELECT name,default_version,installed_version FROM pg_available_extensions WHERE name IN ('plpgsql','unaccent','pg_trgm') ORDER BY 1")
    objects=array("SELECT n.nspname AS schema,c.relname AS name,c.relkind AS kind FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE "+base+" AND c.relkind IN ('v','m','S') ORDER BY 1,2")
    functions=array("SELECT n.nspname AS schema,p.proname AS name,pg_get_function_identity_arguments(p.oid) AS arguments FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE "+base+" AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.classid='pg_proc'::regclass AND d.objid=p.oid AND d.deptype='e') ORDER BY 1,2,3")
    process.stdin.write('ROLLBACK;\n');process.stdin.close();process.wait(timeout=20)
    if process.returncode:raise RuntimeError('Read-only preflight transaction failed.')
    report={'checkedAtUtc':datetime.now(timezone.utc).isoformat(),'clientKind':'native' if args.psql else 'docker','endpointHostSha256':hashlib.sha256(conf['host'].encode()).hexdigest(),'branchReportedByUser':'production','branchNameNotIndependentlyVerifiedBySql':True,'clientTls':{'confirmedByPsqlConninfo':clientTls,'sslmode':'verify-full','channelBinding':'require','serverPgStatSslMayDescribeProxyHop':True},'server':server,'schemas':schemas,'tables':tables,'extensions':extensions,'availableExtensions':available,'otherRelations':objects,'userFunctions':functions,'cloudWritesPerformed':False,'restoreAuthorized':False}
    (out/'neon-preflight.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print(json.dumps({'database':'neondb','serverVersionNum':server['versionNum'],'tls':True,'readOnly':True,'tableCount':len(tables),'rows':sum(t['rows'] for t in tables),'otherRelations':len(objects),'userFunctions':len(functions),'availableRequiredExtensions':[e['name'] for e in available],'createDatabaseObjects':server['createDatabaseObjects'],'cloudWritesPerformed':False,'restoreAuthorized':False}))
except Exception:
    if process.poll() is None:
        try:process.wait(timeout=5)
        except subprocess.TimeoutExpired:pass
    error=process.stderr.read() if process.poll() is not None else 'Read-only query/result validation failed.'
    passline=(private/'pgpass').read_text().strip().split(':',4)
    secret=passline[4].replace('\\:',':').replace('\\\\','\\')
    for value in [conf['host'],conf['user'],secret]:
        if value:error=error.replace(value,'[redacted]')
    (out/'neon-preflight-error.redacted.txt').write_text(error,encoding='utf8')
    raise SystemExit('Neon read-only preflight failed. No restore performed. Redacted diagnostic saved outside Git.') from None
finally:
    if process.poll() is None:
        process.stdin.close();process.wait(timeout=25)
