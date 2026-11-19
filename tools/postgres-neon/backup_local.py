"""Read-only synchronized local audit + custom pg_dump outside the repository."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess

parser=argparse.ArgumentParser()
parser.add_argument('--container',default='culinary-blog-postgres-1')
parser.add_argument('--directory',required=True)
parser.add_argument('--verify-existing',action='store_true')
args=parser.parse_args()
out=Path(args.directory).resolve(); repo=Path(__file__).resolve().parents[2]
if out==repo or repo in out.parents: raise SystemExit('Backup directory must be outside repository.')
if not out.is_dir(): raise SystemExit('Create an ACL-restricted backup directory first.')
archive=out/'culinary-local.dump'
if not args.verify_existing and (archive.exists() or archive.with_suffix('.dump.partial').exists()): raise SystemExit('Backup already exists; not overwritten.')
x=json.loads(subprocess.check_output(['docker','inspect',args.container]))[0]
env=dict(item.split('=',1) for item in x['Config']['Env'] if '=' in item)
database=env['POSTGRES_DB']; user=env['POSTGRES_USER']
def verify_backup():
    report=json.loads((out/'local-snapshot.json').read_text(encoding='utf8'))
    with archive.open('rb') as binary,(out/'archive-list.txt').open('wb') as listing:
        check=subprocess.run(['docker','exec','-i',args.container,'pg_restore','--list'],stdin=binary,stdout=listing,stderr=subprocess.PIPE)
    if check.returncode: raise RuntimeError('pg_restore --list failed; backup not marked verified.')
    with archive.open('rb') as binary,(out/'schema-for-review.sql').open('wb') as sql:
        check=subprocess.run(['docker','exec','-i',args.container,'pg_restore','--schema-only','--no-owner','--no-acl','--file=-'],stdin=binary,stdout=sql,stderr=subprocess.PIPE)
    if check.returncode: raise RuntimeError('Archive schema review extraction failed.')
    with archive.open('rb') as binary: sha=hashlib.file_digest(binary,'sha256').hexdigest()
    manifest={'archive':str(archive),'bytes':archive.stat().st_size,'sha256':sha,'format':'custom','dumpVersion':'16.10','listVerified':True,'tableCount':len(report['tables']),'tableRows':sum(t['rows'] for t in report['tables']),'snapshotAudit':'local-snapshot.json','localWritesPerformed':False,'neonWritesPerformed':False}
    (out/'backup-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(manifest,ensure_ascii=False))
if args.verify_existing:
    verify_backup()
    raise SystemExit()
stderr=(out/'local-audit-errors.log').open('xb')
cmd=['docker','exec','-i',args.container,'psql','-X','-qAt','-v','ON_ERROR_STOP=1','-U',user,'-d',database]
session=subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=stderr,text=True,encoding='utf8')
def line(sql):
    session.stdin.write(sql+'\n'); session.stdin.flush()
    value=session.stdout.readline().strip()
    if not value: raise RuntimeError('Snapshot audit failed; see private error log.')
    return value
def query(sql):return json.loads(line(sql))
def array(sql):return query('SELECT coalesce(json_agg(row_to_json(q)),\'[]\'::json) FROM ('+sql+') q;')
def ident(name):return '"'+name.replace('"','""')+'"'
try:
    session.stdin.write("BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY; SET LOCAL TIME ZONE 'UTC'; SET LOCAL statement_timeout='60s'; SET LOCAL lock_timeout='10s';\n")
    session.stdin.flush()
    snapshot=line('SELECT pg_export_snapshot();')
    if not re.fullmatch('[0-9A-Fa-f-]+',snapshot): raise RuntimeError('Invalid exported snapshot identifier.')
    partial=archive.with_suffix('.dump.partial')
    with partial.open('xb') as binary,(out/'pg-dump-errors.log').open('xb') as errors:
        result=subprocess.run(['docker','exec',args.container,'pg_dump','--no-password','--format=custom','--quote-all-identifiers','--lock-wait-timeout=10000','--snapshot='+snapshot,'-U',user,'-d',database],stdout=binary,stderr=errors)
    if result.returncode: raise RuntimeError('pg_dump failed; partial archive retained and not marked verified.')
    base="n.nspname NOT LIKE 'pg_%' AND n.nspname <> 'information_schema'"
    metadata=query("SELECT json_build_object('version',version(),'versionNum',current_setting('server_version_num'),'database',current_database(),'role',current_user,'bytes',pg_database_size(current_database()),'timezone',current_setting('TimeZone'));")
    schemas=array('SELECT n.nspname AS name FROM pg_namespace n WHERE '+base+' ORDER BY 1')
    extensions=array('SELECT e.extname AS name,e.extversion AS version,n.nspname AS schema,pg_get_userbyid(e.extowner) AS owner FROM pg_extension e JOIN pg_namespace n ON n.oid=e.extnamespace ORDER BY 1')
    tables=array('SELECT n.nspname AS schema,c.relname AS name,c.relkind AS kind FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+" AND c.relkind IN ('r','p') ORDER BY 1,2")
    for table in tables:
        target=ident(table['schema'])+'.'+ident(table['name'])
        table.update(query(f"SELECT json_build_object('rows',count(*),'contentMd5',md5(coalesce(string_agg(digest,'' ORDER BY digest COLLATE \"C\"),''))) FROM (SELECT md5(row_to_json(t)::text) AS digest FROM {target} t) q;"))
    columns=array("SELECT n.nspname AS schema,c.relname AS table,a.attname AS name,a.attnum AS position,format_type(a.atttypid,a.atttypmod) AS type,a.attnotnull AS notnull,a.attidentity AS identity,a.attgenerated AS generated,pg_get_expr(d.adbin,d.adrelid) AS default FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace LEFT JOIN pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum WHERE "+base+" AND c.relkind IN ('r','p') AND a.attnum>0 AND NOT a.attisdropped ORDER BY 1,2,4")
    constraints=array('SELECT n.nspname AS schema,c.relname AS table,k.conname AS name,k.contype AS type,k.convalidated AS validated,pg_get_constraintdef(k.oid) AS definition FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+' ORDER BY 1,2,3')
    indexes=array('SELECT n.nspname AS schema,c.relname AS table,i.relname AS name,x.indisvalid AS valid,pg_get_indexdef(i.oid) AS definition FROM pg_index x JOIN pg_class c ON c.oid=x.indrelid JOIN pg_class i ON i.oid=x.indexrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+' ORDER BY 1,2,3')
    functions=array("SELECT n.nspname AS schema,p.proname AS name,pg_get_function_identity_arguments(p.oid) AS arguments,p.prosecdef AS securitydefiner,pg_get_functiondef(p.oid) AS definition FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE "+base+" AND p.prokind IN ('f','p') AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.classid='pg_proc'::regclass AND d.objid=p.oid AND d.deptype='e') ORDER BY 1,2,3")
    triggers=array('SELECT n.nspname AS schema,c.relname AS table,t.tgname AS name,t.tgenabled AS enabled,pg_get_triggerdef(t.oid) AS definition FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+' AND NOT t.tgisinternal ORDER BY 1,2,3')
    sequences=array("SELECT schemaname AS schema,sequencename AS name,data_type,start_value,min_value,max_value,increment_by,cycle,last_value FROM pg_sequences WHERE schemaname NOT LIKE 'pg_%' ORDER BY 1,2")
    roles=array('SELECT rolname AS name,rolsuper AS superuser,rolcreatedb AS createdb,rolcanlogin AS login FROM pg_roles WHERE rolname !~ \'^pg_\' ORDER BY 1')
    migrations=[]
    if any(t['schema']=='public' and t['name']=='__EFMigrationsHistory' for t in tables):
        migrations=array('SELECT "MigrationId","ProductVersion" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId"')
    largeobjects=query("SELECT json_build_object('count',count(*)) FROM pg_largeobject_metadata;")
    report={'snapshot':snapshot,'metadata':metadata,'schemas':schemas,'extensions':extensions,'tables':tables,'columns':columns,'constraints':constraints,'indexes':indexes,'functions':functions,'triggers':triggers,'sequences':sequences,'roles':roles,'migrations':migrations,'largeObjects':largeobjects,'note':'Table fingerprints use the same read-only exported snapshot as pg_dump. Sequence values are non-MVCC; archive restore values need separate verification.'}
    (out/'local-snapshot.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    session.stdin.write('ROLLBACK;\n'); session.stdin.close(); session.wait(timeout=20)
    if session.returncode: raise RuntimeError('Read-only audit session failed.')
    partial.rename(archive)
    verify_backup()
finally:
    if session.poll() is None:
        session.stdin.close(); session.wait(timeout=20)
    stderr.close()
