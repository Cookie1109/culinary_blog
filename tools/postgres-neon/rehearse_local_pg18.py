"""Restore rehearsal in a new, network-isolated tmpfs container, never Neon/source."""
from pathlib import Path
import argparse,hashlib,json,subprocess,uuid

p=argparse.ArgumentParser();p.add_argument('--directory',required=True);p.add_argument('--image',required=True);args=p.parse_args()
out=Path(args.directory).resolve();archive=out/'culinary-local.dump';source=json.loads((out/'local-snapshot.json').read_text())
manifest=json.loads((out/'backup-manifest.json').read_text())
with archive.open('rb') as f:
    if hashlib.file_digest(f,'sha256').hexdigest()!=manifest['sha256']:raise SystemExit('Archive hash mismatch.')
token=uuid.uuid4().hex[:12];container='culinary-neon-rehearsal-'+token
def run(argv,**kw):
    result=subprocess.run(argv,capture_output=True,**kw)
    if result.returncode:raise RuntimeError('Isolated rehearsal command failed; no user database was modified.')
    return result.stdout
try:
    targetSettings=json.loads((out/'neon-preflight.json').read_text())['server']['databaseSettings']
    if targetSettings['provider']!='b' or targetSettings['locale']!='C.UTF-8':raise RuntimeError('Review target locale before changing rehearsal settings.')
    run(['docker','run','-d','--name',container,'--label','culinary.neon-rehearsal='+token,'--network','none','--tmpfs','/var/lib/postgresql:rw,size=512m','--env','POSTGRES_HOST_AUTH_METHOD=trust','--env','POSTGRES_USER=rehearsal_admin','--env','POSTGRES_DB=culinary_rehearsal','--env','POSTGRES_INITDB_ARGS=--locale=C.UTF-8 --locale-provider=builtin --builtin-locale=C.UTF-8 --encoding=UTF8',args.image])
    run(['docker','exec',container,'sh','-c','for i in $(seq 1 30); do pg_isready -U rehearsal_admin -d culinary_rehearsal >/dev/null 2>&1 && exit 0; sleep 1; done; exit 1'])
    setup='CREATE ROLE restore_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE; ALTER DATABASE culinary_rehearsal OWNER TO restore_owner;'
    run(['docker','exec','-i',container,'psql','-X','-v','ON_ERROR_STOP=1','-U','rehearsal_admin','-d','culinary_rehearsal'],input=setup.encode())
    run(['docker','cp',str(archive),container+':/tmp/source.dump'])
    restore=subprocess.run(['docker','exec',container,'pg_restore','--exit-on-error','--single-transaction','--no-owner','--no-acl','--role=restore_owner','-U','rehearsal_admin','-d','culinary_rehearsal','/tmp/source.dump'],capture_output=True)
    (out/'pg18-rehearsal-restore.log').write_bytes(restore.stdout+restore.stderr)
    if restore.returncode:raise RuntimeError('PG18 restore rehearsal failed; private log saved. Neon untouched.')
    proc=subprocess.Popen(['docker','exec','-i',container,'psql','-X','-qAt','-v','ON_ERROR_STOP=1','-U','rehearsal_admin','-d','culinary_rehearsal'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf8')
    def query(sql):
        proc.stdin.write(sql+'\n');proc.stdin.flush()
        line=proc.stdout.readline().strip()
        if not line:raise RuntimeError('Rehearsal verification query failed.')
        return json.loads(line)
    def ident(x):return '"'+x.replace('"','""')+'"'
    proc.stdin.write("SET ROLE restore_owner; SET TIME ZONE 'UTC';\n");proc.stdin.flush()
    tableChecks=[]
    for table in source['tables']:
        target=ident(table['schema'])+'.'+ident(table['name'])
        actual=query(f"SELECT json_build_object('rows',count(*),'contentMd5',md5(coalesce(string_agg(digest,'' ORDER BY digest COLLATE \"C\"),''))) FROM (SELECT md5(row_to_json(t)::text) AS digest FROM {target} t) q;")
        matches=actual['rows']==table['rows'] and actual['contentMd5']==table['contentMd5']
        tableChecks.append({'schema':table['schema'],'table':table['name'],'rows':actual['rows'],'contentHashMatches':matches})
    base="n.nspname NOT LIKE 'pg_%' AND n.nspname <> 'information_schema'"
    counts=query("SELECT json_build_object('constraints',(SELECT count(*) FROM pg_constraint k JOIN pg_namespace n ON n.oid=k.connamespace WHERE "+base+"),'invalidConstraints',(SELECT count(*) FROM pg_constraint k JOIN pg_namespace n ON n.oid=k.connamespace WHERE "+base+" AND NOT k.convalidated),'indexes',(SELECT count(*) FROM pg_index x JOIN pg_class c ON c.oid=x.indrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE "+base+"),'invalidIndexes',(SELECT count(*) FROM pg_index x JOIN pg_class c ON c.oid=x.indrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE "+base+" AND NOT x.indisvalid),'sequences',(SELECT count(*) FROM pg_sequences WHERE schemaname NOT LIKE 'pg_%'));" )
    actualColumns=query("SELECT coalesce(json_agg(row_to_json(t)),'[]'::json) FROM (SELECT n.nspname AS schema,c.relname AS table,a.attname AS name,a.attnum AS position,format_type(a.atttypid,a.atttypmod) AS type,a.attnotnull AS notnull,a.attidentity AS identity,a.attgenerated AS generated,pg_get_expr(d.adbin,d.adrelid) AS default FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace LEFT JOIN pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum WHERE "+base+" AND c.relkind IN ('r','p') AND a.attnum>0 AND NOT a.attisdropped ORDER BY 1,2,4) t;")
    actualConstraints=query("SELECT coalesce(json_agg(row_to_json(t)),'[]'::json) FROM (SELECT n.nspname AS schema,c.relname AS table,k.conname AS name,k.contype AS type,k.convalidated AS validated,pg_get_constraintdef(k.oid) AS definition FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE "+base+" AND k.contype<>'n' ORDER BY 1,2,3) t;")
    notNullConstraints=query("SELECT to_json(count(*)) FROM pg_constraint k JOIN pg_namespace n ON n.oid=k.connamespace WHERE "+base+" AND k.contype='n';")
    structure={'columnDefinitionsMatch':actualColumns==source['columns'],'logicalConstraintsExcludingPg18NotNullMatch':actualConstraints==source['constraints'],'pg18NotNullCatalogConstraints':notNullConstraints,'sourceNotNullColumns':sum(c['notnull'] for c in source['columns']),'indexesCountMatches':counts['indexes']==len(source['indexes']),'sequencesCountMatches':counts['sequences']==len(source['sequences'])}
    foreign=query("SELECT coalesce(json_agg(row_to_json(t)),'[]'::json) FROM (SELECT n.nspname AS schema,c.relname AS table,rn.nspname AS refschema,rc.relname AS reftable,ARRAY(SELECT a.attname FROM unnest(k.conkey) WITH ORDINALITY v(attnum,ord) JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum=v.attnum ORDER BY v.ord) AS columns,ARRAY(SELECT a.attname FROM unnest(k.confkey) WITH ORDINALITY v(attnum,ord) JOIN pg_attribute a ON a.attrelid=rc.oid AND a.attnum=v.attnum ORDER BY v.ord) AS refcolumns FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_class rc ON rc.oid=k.confrelid JOIN pg_namespace rn ON rn.oid=rc.relnamespace WHERE k.contype='f' AND "+base+") t;")
    fkOrphans=0
    for fk in foreign:
        target=ident(fk['schema'])+'.'+ident(fk['table']);ref=ident(fk['refschema'])+'.'+ident(fk['reftable'])
        nonnull=' AND '.join('t.'+ident(x)+' IS NOT NULL' for x in fk['columns'])
        equal=' AND '.join('t.'+ident(a)+'=r.'+ident(b) for a,b in zip(fk['columns'],fk['refcolumns']))
        fkOrphans+=query(f'SELECT to_json(count(*)) FROM {target} t WHERE {nonnull} AND NOT EXISTS (SELECT 1 FROM {ref} r WHERE {equal});')
    owned=query("SELECT coalesce(json_agg(row_to_json(t)),'[]'::json) FROM (SELECT ns.nspname AS schema,s.relname AS name,nt.nspname AS tableschema,c.relname AS table,a.attname AS column FROM pg_class s JOIN pg_namespace ns ON ns.oid=s.relnamespace JOIN pg_depend d ON d.classid='pg_class'::regclass AND d.objid=s.oid AND d.deptype IN ('a','i') JOIN pg_class c ON c.oid=d.refobjid JOIN pg_namespace nt ON nt.oid=c.relnamespace JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum=d.refobjsubid WHERE s.relkind='S') t;")
    sequenceChecks=[]
    for seq in owned:
        relation=ident(seq['schema'])+'.'+ident(seq['name']);table=ident(seq['tableschema'])+'.'+ident(seq['table']);column=ident(seq['column'])
        check=query(f"SELECT json_build_object('last',last_value,'called',is_called,'maximum',(SELECT max({column}) FROM {table})) FROM {relation};")
        check['safe']=check['maximum'] is None or (check['last']>=check['maximum'] and check['called'])
        sequenceChecks.append({'schema':seq['schema'],'sequence':seq['name'],**check})
    fts=query("SELECT json_build_object('unaccent',unaccent('phở')='pho','trigram',similarity('pho','pho')=1,'fts',(SELECT count(*) FROM public.\"Recipes\" WHERE \"SearchVector\" @@ plainto_tsquery('simple',unaccent('cơm'))));")
    fts['storedNormalizationMismatches']=query("SELECT to_json(count(*)) FROM public.\"Recipes\" WHERE \"SearchTitle\" IS DISTINCT FROM lower(unaccent(coalesce(\"Title\",''))) OR \"SearchVector\" IS DISTINCT FROM (setweight(to_tsvector('simple',lower(unaccent(coalesce(\"Title\",'')))),'A') || setweight(to_tsvector('simple',unaccent(coalesce(\"Description\",''))),'B'));")
    proc.stdin.write('BEGIN; UPDATE public."Recipes" SET "Title"="Title" WHERE "Id"=(SELECT "Id" FROM public."Recipes" LIMIT 1); ROLLBACK;\n');proc.stdin.flush()
    role=query("SELECT json_build_object('superuser',(SELECT rolsuper FROM pg_roles WHERE rolname=current_user),'version',current_setting('server_version_num'));")
    proc.stdin.close();proc.wait(timeout=20)
    if proc.returncode:raise RuntimeError('Rehearsal trigger verification failed.')
    report={'target':'new isolated local PostgreSQL18 tmpfs only','targetDatabaseSettings':targetSettings,'container':container,'server':role,'restoreNoOwnerNoAclSingleTransaction':True,'tables':tableChecks,'counts':counts,'structure':structure,'foreignKeysChecked':len(foreign),'fkOrphans':fkOrphans,'ownedSequenceChecks':sequenceChecks,'fts':fts,'triggerUpdateRolledBack':True,'neonWritesPerformed':False,'sourceWritesPerformed':False}
    report['passed']=all(t['contentHashMatches'] for t in tableChecks) and all(structure[k] for k in ['columnDefinitionsMatch','logicalConstraintsExcludingPg18NotNullMatch','indexesCountMatches','sequencesCountMatches']) and not counts['invalidConstraints'] and not counts['invalidIndexes'] and fkOrphans==0 and all(v['safe'] for v in sequenceChecks) and fts['unaccent'] and fts['trigram'] and not fts['storedNormalizationMismatches'] and not role['superuser']
    (out/'pg18-rehearsal.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    if not report['passed']:raise RuntimeError('Rehearsal comparison mismatch; review private report.')
    print(json.dumps({'isolatedPg18Restore':'passed','nonSuperuser':True,'tablesMatched':len(tableChecks),'rowsMatched':sum(t['rows'] for t in tableChecks),'fkOrphans':fkOrphans,'ownedSequencesSafe':len(sequenceChecks),'ftsAndTrigger':'passed','neonWritesPerformed':False,'sourceWritesPerformed':False}))
finally:
    check=json.loads(subprocess.check_output(['docker','inspect',container]))[0]
    if check['Config']['Labels'].get('culinary.neon-rehearsal')!=token:raise RuntimeError('Cleanup identity mismatch.')
    subprocess.run(['docker','rm','-f',container],stdout=subprocess.DEVNULL,check=True)
