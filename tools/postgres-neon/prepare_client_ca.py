"""Provide a verified public CA to an isolated client image missing its CA bundle."""
from pathlib import Path
import hashlib,ssl,sys,urllib.request

private=Path(sys.argv[1]).resolve()/'neon-private'
repo=Path(__file__).resolve().parents[2]
if private==repo or repo in private.parents:raise SystemExit('Private client directory must be outside Git.')
if not private.is_dir():raise SystemExit('Create the ACL-restricted connection directory first.')
pem=urllib.request.urlopen('https://letsencrypt.org/certs/isrgrootx1.pem',timeout=20).read().decode('ascii')
if hashlib.sha256(ssl.PEM_cert_to_DER_cert(pem)).hexdigest()!='96bcec06264976f37460779acf28c5a7cfe8a3c0aae11a8ffcee05c0bddf08c6':raise SystemExit('Official CA fingerprint mismatch.')
(private/'isrg-root-x1.pem').write_text(pem,encoding='ascii',newline='\n')
print('ISRG Root X1 verified; private client trust file prepared. No system trust or database changed.')
