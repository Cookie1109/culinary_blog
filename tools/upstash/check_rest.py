"""Check supplied Upstash REST credential and TCP TLS reachability; no cache writes."""
from pathlib import Path
from datetime import datetime,timezone
import argparse,hashlib,json,socket,ssl,urllib.error,urllib.parse,urllib.request

p=argparse.ArgumentParser();p.add_argument('--env-file',required=True,type=Path)
p.add_argument('--verify-supplied-rest-token',action='store_true');a=p.parse_args()
values=dict(line.split('=',1) for line in a.env_file.read_text(encoding='utf8').splitlines() if line and not line.startswith('#'))
url=urllib.parse.urlsplit(values['UPSTASH_REDIS_REST_URL'])
if url.scheme!='https' or not url.hostname or not url.hostname.endswith('.upstash.io') or url.path not in ('','/') or url.username:
    raise SystemExit('Expected HTTPS Upstash database URL.')
report={'checkedAtUtc':datetime.now(timezone.utc).isoformat(),'endpointSha256':hashlib.sha256(url.hostname.encode()).hexdigest(),'restPing':False,'restWritesPerformed':False,'tcpTlsCertificateVerified':False,'tcpAuthenticationTested':False,'tcpPasswordProvided':bool(values.get('UPSTASH_REDIS_PASSWORD')),'renderChanged':False}
try:
    token=values['UPSTASH_REDIS_PASSWORD'] if a.verify_supplied_rest_token else values['UPSTASH_REDIS_REST_TOKEN']
    request=urllib.request.Request(values['UPSTASH_REDIS_REST_URL'],data=b'["PING"]',headers={'Authorization':'Bearer '+token,'Content-Type':'application/json'},method='POST')
    with urllib.request.urlopen(request,timeout=15) as response:
        report['restHttpStatus']=response.status
        report['restPing']=json.load(response).get('result')=='PONG'
except urllib.error.HTTPError as e:report['restHttpStatus']=e.code
except Exception as e:report['restFailureType']=type(e).__name__
try:
    context=ssl.create_default_context()
    with socket.create_connection((url.hostname,6379),timeout=10) as sock:
        with context.wrap_socket(sock,server_hostname=url.hostname) as tls:
            report.update(tcpTlsCertificateVerified=True,tcpTlsVersion=tls.version())
except Exception as e:report['tcpTlsFailureType']=type(e).__name__
if a.verify_supplied_rest_token and report['restPing']:
    values['UPSTASH_REDIS_REST_TOKEN']=token
    temporary=a.env_file.with_suffix('.env.new')
    with temporary.open('x',encoding='utf8',newline='\n') as f:
        for key,value in values.items():f.write(f'{key}={value}\n')
    temporary.replace(a.env_file)
    report['suppliedTokenVerifiedAndSaved']=True
(a.env_file.parent/'rest-check.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
print(json.dumps(report))
