"""Verify approved Neon restore using one repeatable-read, read-only transaction."""
from pathlib import Path
from datetime import datetime,timezone
import argparse,hashlib,json,os,ssl,subprocess

p=argparse.ArgumentParser();p.add_argument('--directory',type=Path,required=True);p.add_argument('--psql',type=Path,required=True);a=p.parse_args()
out=a.directory.resolve();private=out/'neon-private'
source=json.loads((out/'local-snapshot.json').read_text())
rehearsal=json.loads((out/'pg18-rehearsal.json').read_text())
execution=json.loads((out/'neon-restore-execution.json').read_text())
if not execution['restoreCommitted']:raise SystemExit('No committed approved restore; verification only, never retry restore.')
conf=dict(line.split('=',1) for line in (private/'pg_service.conf').read_text().splitlines() if '=' in line)
if hashlib.sha256(conf['host'].encode()).hexdigest()!=execution['endpointHostSha256'] or conf['dbname']!='neondb' or conf.get('sslmode')!='verify-full' or conf.get('channel_binding')!='require':
    raise SystemExit('Verification endpoint/TLS configuration changed.')
if hashlib.sha256(ssl.PEM_cert_to_DER_cert((private/'isrg-root-x1.pem').read_text())).hexdigest()!='96bcec06264976f37460779acf28c5a7cfe8a3c0aae11a8ffcee05c0bddf08c6':
    raise SystemExit('Client CA mismatch.')
environment=os.environ.copy()
for key in ['PGHOST','PGHOSTADDR','PGPORT','PGDATABASE','PGUSER','PGPASSWORD','PGSSLMODE','PGSSLROOTCERT','PGCHANNELBINDING','PGOPTIONS']:
    environment.pop(key,None)
environment.update(PGSERVICE='neon_readonly',PGSERVICEFILE=str(private/'pg_service.conf'),PGPASSFILE=str(private/'pgpass'),SSL_CERT_FILE=str(private/'isrg-root-x1.pem'),PGCLIENTENCODING='UTF8',PGOPTIONS='-c default_transaction_read_only=on -c statement_timeout=30000 -c lock_timeout=5000')
proc=subprocess.Popen([str(a.psql),'-X','-w','-qAt','-v','ON_ERROR_STOP=1'],env=environment,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf8')
def query(sql):
    proc.stdin.write(sql+'\n');proc.stdin.flush()
    line=proc.stdout.readline().strip()
    if not line:raise RuntimeError('Read-only verification failed; provider detail withheld.')
    return json.loads(line)
def ident(value):return '"'+value.replace('"','""')+'"'
def array(sql):return query("SELECT coalesce(json_agg(row_to_json(q)),'[]'::json) FROM ("+sql+") q;")
try:
    proc.stdin.write("BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY; SET LOCAL TIME ZONE 'UTC';\n");proc.stdin.flush()
    identity=query("SELECT json_build_object('database',current_database(),'readOnly',current_setting('transaction_read_only'),'version',current_setting('server_version_num'));")
    if identity['database']!='neondb' or identity['readOnly']!='on':raise RuntimeError('Unexpected verification database/session.')
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
    fts=query("SELECT json_build_object('unaccent',unaccent('ph\u1edf')='pho','trigram',similarity('pho','pho')=1,'fts',(SELECT count(*) FROM public.\"Recipes\" WHERE \"SearchVector\" @@ plainto_tsquery('simple',unaccent('c\u01a1m'))));")
    fts['storedNormalizationMismatches']=query("SELECT to_json(count(*)) FROM public.\"Recipes\" WHERE \"SearchTitle\" IS DISTINCT FROM lower(unaccent(coalesce(\"Title\",''))) OR \"SearchVector\" IS DISTINCT FROM (setweight(to_tsvector('simple',lower(unaccent(coalesce(\"Title\",'')))),'A') || setweight(to_tsvector('simple',unaccent(coalesce(\"Description\",''))),'B'));")
    actualTables=array("SELECT n.nspname AS schema,c.relname AS name,c.relkind AS kind FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE "+base+" AND c.relkind IN ('r','p') ORDER BY 1,2")
    structure['tableInventoryMatches']=actualTables==[{k:t[k] for k in ['schema','name','kind']} for t in source['tables']]
    indexes=array('SELECT n.nspname AS schema,c.relname AS table,i.relname AS name,x.indisvalid AS valid,pg_get_indexdef(i.oid) AS definition FROM pg_index x JOIN pg_class c ON c.oid=x.indrelid JOIN pg_class i ON i.oid=x.indexrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+' ORDER BY 1,2,3')
    functions=array("SELECT n.nspname AS schema,p.proname AS name,pg_get_function_identity_arguments(p.oid) AS arguments,p.prosecdef AS securitydefiner,pg_get_functiondef(p.oid) AS definition FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE "+base+" AND p.prokind IN ('f','p') AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.classid='pg_proc'::regclass AND d.objid=p.oid AND d.deptype='e') ORDER BY 1,2,3")
    triggers=array('SELECT n.nspname AS schema,c.relname AS table,t.tgname AS name,t.tgenabled AS enabled,pg_get_triggerdef(t.oid) AS definition FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE '+base+' AND NOT t.tgisinternal ORDER BY 1,2,3')
    extensions=array('SELECT e.extname AS name,e.extversion AS version,n.nspname AS schema FROM pg_extension e JOIN pg_namespace n ON n.oid=e.extnamespace ORDER BY 1')
    migrations=array('SELECT "MigrationId","ProductVersion" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId"')
    structure.update(indexDefinitionsMatch=indexes==source['indexes'],functionsMatch=functions==source['functions'],triggersMatch=triggers==source['triggers'],extensionsMatch=extensions==[{k:e[k] for k in ['name','version','schema']} for e in source['extensions']],migrationHistoryMatch=migrations==source['migrations'])
    sequenceChecks.sort(key=lambda s:(s['schema'],s['sequence']))
    expectedSequences=sorted(rehearsal['ownedSequenceChecks'],key=lambda s:(s['schema'],s['sequence']))
    structure['sequenceValuesMatchArchiveRehearsal']=sequenceChecks==expectedSequences
    proc.stdin.write('ROLLBACK;\n');proc.stdin.close();proc.wait(timeout=20)
    if proc.returncode:raise RuntimeError('Read-only verification transaction failed.')
    boolFields=['columnDefinitionsMatch','logicalConstraintsExcludingPg18NotNullMatch','indexesCountMatches','sequencesCountMatches','tableInventoryMatches','indexDefinitionsMatch','functionsMatch','triggersMatch','extensionsMatch','migrationHistoryMatch','sequenceValuesMatchArchiveRehearsal']
    passed=all(t['contentHashMatches'] for t in tableChecks) and all(structure[k] for k in boolFields) and structure['pg18NotNullCatalogConstraints']==structure['sourceNotNullColumns'] and not counts['invalidConstraints'] and not counts['invalidIndexes'] and fkOrphans==0 and len(foreign)==14 and all(s['safe'] for s in sequenceChecks) and fts['unaccent'] and fts['trigram'] and fts['fts']==rehearsal['fts']['fts'] and not fts['storedNormalizationMismatches']
    report={'checkedAtUtc':datetime.now(timezone.utc).isoformat(),'target':'Neon neondb','session':identity,'passed':passed,'tables':tableChecks,'rowCount':sum(t['rows'] for t in tableChecks),'counts':counts,'structure':structure,'foreignKeysChecked':len(foreign),'fkOrphans':fkOrphans,'ownedSequenceChecks':sequenceChecks,'fts':fts,'verificationWritesPerformed':False,'sourceWritesPerformed':False,'renderSwitchPerformed':False}
    (out/'neon-restore-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    if not passed:raise RuntimeError('Comparison mismatch; inspect private verification report. Do not change application connection.')
    print(json.dumps({'neonRestoreVerification':'passed','tablesMatched':len(tableChecks),'rowsMatched':report['rowCount'],'schemaMatches':True,'fkOrphans':fkOrphans,'indexesValid':counts['indexes'],'sequencesSafe':len(sequenceChecks),'migrationsRetained':len(migrations),'verificationReadOnly':True,'sourceChanged':False,'renderChanged':False}))
except Exception:
    if proc.poll() is None:
        proc.terminate()
        try:proc.wait(timeout=10)
        except subprocess.TimeoutExpired:proc.kill();proc.wait()
    raise SystemExit('Neon verification failed; no additional writes. Review report and restore journal, do not rerun restore.') from None
finally:
    if proc.poll() is None:proc.terminate();proc.wait(timeout=10)
