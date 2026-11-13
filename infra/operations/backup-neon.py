"""Read-only PostgreSQL backup, encrypted local archive and isolated restore check."""

import argparse
import datetime as dt
import hashlib
import json
import re
import secrets
import shutil
import subprocess
import time
from pathlib import Path

RUN = re.compile(r"\d{8}T\d{6}Z-[a-f0-9]{8}\Z")
JOB = "culinary-g06-postgres"


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def expired_run(name, receipt, now):
    if not RUN.fullmatch(name) or receipt.get("job") != JOB or receipt.get("status") != "success":
        return False
    try:
        created = dt.datetime.strptime(name[:16], "%Y%m%dT%H%M%SZ").replace(tzinfo=dt.timezone.utc)
    except ValueError:
        return False
    return created < now - dt.timedelta(days=30)


def ident(value):
    return '"' + value.replace('"', '""') + '"'


def main(config_path):
    cfg = json.loads(config_path.read_text(encoding="utf-8-sig"))
    root = Path(cfg["privateRoot"]).resolve()
    if root != Path("D:/Culinary_Backups/G06/postgres-scheduled").resolve():
        raise ValueError("Unexpected backup directory")
    if digest(Path(__file__)) != cfg["scriptSha256"]:
        raise ValueError("Backup script changed; review before running the scheduled job")
    if shutil.disk_usage(root).free < 1024**3:
        raise RuntimeError("Less than1GiB available for backup verification")
    now = dt.datetime.now(dt.timezone.utc)
    run_id = now.strftime("%Y%m%dT%H%M%SZ") + "-" + secrets.token_hex(4)
    work = root / "runs" / run_id
    work.mkdir(parents=True)
    bundle = work / "bundle"
    bundle.mkdir()
    log = work / "operations.private.log"
    receipt = {"job": JOB, "runId": run_id, "startedAtUtc": now.isoformat(), "status": "running"}
    pg = Path(cfg["pgPrivate"])
    image = cfg["postgresImage"]
    if not re.fullmatch(r"sha256:[a-f0-9]{64}", image):
        raise ValueError("Expected pinned PostgreSQL client image")
    common = ["docker", "run", "--rm", "-i", "--pull=never", "--read-only", "--user", "1000:1000",
              "--tmpfs", "/tmp:rw,noexec,nosuid,size=16m", "--cpus", "0.5", "--memory", "512m",
              "--mount", f"type=bind,src={pg},dst=/run/neon,readonly",
              "--env", "PGSERVICE=neon_readonly", "--env", "PGSERVICEFILE=/run/neon/pg_service.conf",
              "--env", "SSL_CERT_FILE=/run/neon/isrg-root-x1.pem",
              "--env", "PGOPTIONS=-c default_transaction_read_only=on -c statement_timeout=120000 -c lock_timeout=10000"]
    auth = "cp /run/neon/pgpass /tmp/pgpass; chmod 600 /tmp/pgpass; export PGPASSFILE=/tmp/pgpass; "
    session = None
    restore_name = "culinary-g06-pgverify-" + run_id.lower()
    restore_owned = False
    creation = getattr(subprocess, "CREATE_NO_WINDOW", 0)

    def command(args, stdin=None, capture=False, timeout=240):
        with log.open("ab") as err:
            result = subprocess.run([str(x) for x in args], input=stdin, stdout=subprocess.PIPE if capture else err,
                                    stderr=err, timeout=timeout, creationflags=creation)
        if result.returncode:
            raise RuntimeError("Backup operation failed; diagnostic retained in private log")
        return result.stdout if capture else None

    def query(sql):
        session.stdin.write(sql + "\n")
        session.stdin.flush()
        value = session.stdout.readline().strip()
        if not value:
            raise RuntimeError("Read-only snapshot session failed")
        return value

    def local(sql):
        return command(["docker", "exec", restore_name, "psql", "-h", "127.0.0.1", "-U", "g06",
                        "-d", "neondb", "-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-c", sql], capture=True).decode().strip()

    def row_query(table):
        return "SELECT json_build_object('count',count(*),'hash',md5(coalesce(string_agg(row_to_json(x)::text,'' ORDER BY row_to_json(x)::text),''))) FROM " + table + " x;"

    def clean_scratch(path):
        if path.resolve().parent != work.resolve() or path.name not in ["bundle", "verified"] or path.is_symlink():
            raise ValueError("Unsafe scratch directory")
        if not work.resolve().is_relative_to((root / "runs").resolve()):
            raise ValueError("Unexpected scratch parent")
        shutil.rmtree(path)

    try:
        with log.open("ab") as err:
            session = subprocess.Popen(common + ["--entrypoint", "sh", image, "-ec", auth + "exec psql -X -w -qAt -v ON_ERROR_STOP=1"],
                                       stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=err, text=True, encoding="utf-8", creationflags=creation)
            session.stdin.write("BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY; SET LOCAL TIME ZONE 'UTC';\n")
            session.stdin.flush()
            snapshot = query("SELECT pg_export_snapshot();")
            if not re.fullmatch(r"[A-Fa-f0-9-]+", snapshot):
                raise RuntimeError("Invalid exported snapshot")
            server = json.loads(query("SELECT json_build_object('database',current_database(),'readOnly',current_setting('transaction_read_only'),'walLevel',current_setting('wal_level'),'fsync',current_setting('fsync'),'synchronousCommit',current_setting('synchronous_commit'),'synchronousStandbyNames',current_setting('synchronous_standby_names'));"))
            if server["database"] != "neondb" or server["readOnly"] != "on":
                raise RuntimeError("Unexpected database or writable snapshot")
            # Neon persists WAL through the walproposer/safekeeper quorum, not compute-local fsync.
            if server["synchronousStandbyNames"] != "walproposer" or server["synchronousCommit"] != "on" or server["walLevel"] not in ["replica", "logical"]:
                raise RuntimeError("Database durability settings require review")
            tables = json.loads(query("SELECT json_agg(row_to_json(t)) FROM (SELECT schemaname,tablename FROM pg_tables WHERE schemaname NOT LIKE 'pg_%' AND schemaname<>'information_schema' ORDER BY 1,2)t;"))
            expected = {}
            for table in tables:
                name = table["schemaname"] + "." + table["tablename"]
                expected[name] = json.loads(query(row_query(ident(table["schemaname"]) + "." + ident(table["tablename"]))))
            migrations = json.loads(query('SELECT json_agg("MigrationId" ORDER BY "MigrationId") FROM "__EFMigrationsHistory";'))
            dump = bundle / "neon.dump"
            with dump.open("xb") as output:
                result = subprocess.run(common + ["--entrypoint", "sh", image, "-ec", auth + "exec pg_dump -w --format=custom --no-owner --no-acl --quote-all-identifiers --snapshot=" + snapshot],
                                        stdout=output, stderr=err, timeout=240, creationflags=creation)
            if result.returncode:
                raise RuntimeError("Read-only pg_dump failed")
            session.stdin.write("ROLLBACK;\n")
            session.stdin.close()
            session.wait(timeout=20)
            if session.returncode:
                raise RuntimeError("Snapshot session did not finish cleanly")
        (bundle / "snapshot.private.json").write_text(json.dumps({"tables": expected, "migrations": migrations, "server": server}))
        shutil.copyfile(cfg["certificateFile"], bundle / "data-protection-certificate.base64")
        started_restore = time.monotonic()
        command(["docker", "run", "-d", "--name", restore_name, "--label", "culinary.g06.pgverify=" + run_id,
                 "--network", "none", "--tmpfs", "/var/lib/postgresql:rw,size=512m,uid=70,gid=70,mode=0700", "--memory", "768m", "--cpus", "1",
                 "--env", "POSTGRES_INITDB_ARGS=--locale-provider=builtin --locale=C.UTF-8", "--env", "POSTGRES_USER=g06",
                 "--env", "POSTGRES_DB=neondb", "--env", "POSTGRES_HOST_AUTH_METHOD=trust", image])
        restore_owned = True
        for _ in range(90):
            probe = subprocess.run(["docker", "exec", restore_name, "pg_isready", "-h", "127.0.0.1", "-U", "g06", "-d", "neondb"], capture_output=True, creationflags=creation)
            if probe.returncode == 0:
                break
            time.sleep(1)
        else:
            raise RuntimeError("Isolated restore database not ready")
        command(["docker", "exec", "-i", restore_name, "pg_restore", "-h", "127.0.0.1", "-U", "g06", "-d", "neondb", "--no-owner", "--no-acl", "--exit-on-error"], stdin=dump.read_bytes())
        for table in tables:
            name = table["schemaname"] + "." + table["tablename"]
            actual = json.loads(local("SET TIME ZONE 'UTC'; " + row_query(ident(table["schemaname"]) + "." + ident(table["tablename"]))))
            if actual != expected[name]:
                raise RuntimeError("Restored rows/checksum mismatch")
        restored_migrations = json.loads(local('SELECT json_agg("MigrationId" ORDER BY "MigrationId") FROM "__EFMigrationsHistory";'))
        if restored_migrations != migrations:
            raise RuntimeError("Restored schema version mismatch")
        keyring = json.loads(local('SELECT json_build_object(\'count\',count(*),\'encrypted\',bool_and("Xml" LIKE \'%encryptedSecret%\'),\'plaintext\',bool_or("Xml" LIKE \'%<masterKey%\')) FROM "DataProtectionKeys";'))
        if not keyring["count"] or not keyring["encrypted"] or keyring["plaintext"]:
            raise RuntimeError("Restored keyring not encrypted")
        restore_seconds = round(time.monotonic() - started_restore, 3)
        hashes = {p.name: digest(p) for p in bundle.iterdir()}
        (bundle / "manifest.json").write_text(json.dumps(hashes))
        archive = work / ("postgres-" + run_id + ".7z")
        password = Path(cfg["archiveKey"]).read_text().strip()
        if not re.fullmatch(r"[a-f0-9]{64}", password):
            raise RuntimeError("Reviewed recovery key missing")
        response = (password + "\n" + password + "\n").encode()
        command([cfg["sevenZip"], "a", "-t7z", "-mhe=on", "-mx=1", "-p", archive, bundle / "*"], stdin=response)
        command([cfg["sevenZip"], "t", archive], stdin=response)
        verified = work / "verified"
        command([cfg["sevenZip"], "x", archive, "-o" + str(verified)], stdin=response)
        for name, expected_hash in hashes.items():
            if digest(verified / name) != expected_hash:
                raise RuntimeError("Encrypted archive roundtrip mismatch")
        receipt.update({"status": "success", "sourceReadOnly": True, "sourceWrites": False,
                        "snapshotTables": len(tables), "snapshotRows": sum(x["count"] for x in expected.values()),
                        "migrations": len(migrations), "rowHashesAndSchemaMatch": True, "sameExportedSnapshot": True,
                        "keyring": keyring, "durability": server, "restoreVerifySeconds": restore_seconds,
                        "archiveBytes": archive.stat().st_size, "archiveSha256": digest(archive), "encryptedFilesVerified": len(hashes)})
        clean_scratch(bundle)
        clean_scratch(verified)
        expired = 0
        for previous in (root / "runs").iterdir():
            result_file = previous / "result.json"
            if previous.is_symlink() or not previous.is_dir() or not RUN.fullmatch(previous.name) or not result_file.is_file():
                continue
            if not expired_run(previous.name, json.loads(result_file.read_text()), now):
                continue
            allowed = {"result.json", "operations.private.log", "postgres-" + previous.name + ".7z"}
            children = list(previous.iterdir())
            if previous.resolve().parent != (root / "runs").resolve() or any(p.is_symlink() or not p.is_file() or p.name not in allowed for p in children):
                continue
            for path in children:
                path.unlink()
            previous.rmdir()
            expired += 1
        receipt["expiredOwnRuns"] = expired
    except Exception as error:
        receipt.update({"status": "failed", "errorType": type(error).__name__})
        raise
    finally:
        if session is not None and session.poll() is None:
            session.terminate()
            session.wait(timeout=20)
        if restore_owned:
            label = command(["docker", "inspect", restore_name, "--format", '{{index .Config.Labels "culinary.g06.pgverify"}}'], capture=True).decode().strip()
            if label != run_id:
                raise RuntimeError("Restore cleanup identity mismatch")
            command(["docker", "rm", "-f", "-v", restore_name])
            receipt["isolatedRestoreRemoved"] = True
        receipt["finishedAtUtc"] = dt.datetime.now(dt.timezone.utc).isoformat()
        (work / "result.json").write_text(json.dumps(receipt, indent=2))
        (root / "last-run.json").write_text(json.dumps(receipt, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", required=True, type=Path)
    main(parser.parse_args().config)
