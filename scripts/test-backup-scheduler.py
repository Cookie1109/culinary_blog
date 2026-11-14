"""Execute the POSIX scheduler with a controlled clock inside its PostgreSQL image."""
import pathlib
import subprocess
import tempfile
import unittest
import uuid

ROOT = pathlib.Path(__file__).resolve().parents[1]
IMAGE = "postgres:16.10-bookworm"


class BackupSchedulerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # Download the fixture before starting each scenario's scheduler timeout.
        image = subprocess.run(
            ["docker", "image", "inspect", "--format", "{{.Id}}", IMAGE],
            capture_output=True, text=True, timeout=15,
        )
        if image.returncode:
            subprocess.run(["docker", "pull", IMAGE], check=True, timeout=180)

    def scenario(self, start, runtime=0, failures=0):
        with tempfile.TemporaryDirectory(dir=ROOT / "tmp") as directory:
            work = pathlib.Path(directory)
            (work / "clock").write_text(str(start), encoding="utf-8", newline="\n")
            (work / "date").write_text('#!/bin/sh\ncat /test/clock\n', encoding="utf-8", newline="\n")
            (work / "sleep").write_text('''#!/bin/sh
echo "sleep:$1" >> /test/events
count=$(grep -c '^sleep:' /test/events)
if test "$count" -gt 2; then exit 95; fi
now=$(cat /test/clock)
echo $((now + $1)) > /test/clock
''', encoding="utf-8", newline="\n")
            (work / "job").write_text(f'''#!/bin/sh
now=$(cat /test/clock)
echo "job:$now" >> /test/events
count=$(grep -c '^job:' /test/events)
echo $((now + {runtime})) > /test/clock
test "$count" -gt {failures}
''', encoding="utf-8", newline="\n")
            container = "culinary-g06-scheduler-" + uuid.uuid4().hex[:12]
            try:
                result = subprocess.run([
                    "docker", "run", "--rm", "--name", container, "--label", "culinary.g06=scheduler", "--network", "none",
                    "-v", f"{work.as_posix()}:/test",
                    "-v", f"{(ROOT / 'infra/operations/backup-scheduler.sh').as_posix()}:/scheduler:ro",
                    IMAGE, "/bin/sh", "-ec",
                    "chmod +x /test/date /test/sleep; PATH=/test:$PATH /bin/sh /scheduler /test/job",
                ], capture_output=True, text=True, timeout=30)
            finally:
                try:
                    subprocess.run(["docker", "rm", "-f", container], capture_output=True, timeout=5, check=False)
                except subprocess.TimeoutExpired:
                    pass  # A failed Docker daemon must not hide the original test failure.
            self.assertEqual(result.returncode, 95, result.stderr)
            return (work / "events").read_text(encoding="utf-8").splitlines()

    def test_utc_boundary_and_restart_do_not_run_immediately(self):
        for start, delay in [(10740, 60), (10801, 86399), (0, 10800), (86399, 10801)]:
            with self.subTest(start=start):
                self.assertEqual(self.scenario(start)[0], f"sleep:{delay}")

    def test_job_duration_does_not_shift_tomorrows_run(self):
        self.assertEqual(self.scenario(10740, runtime=120)[:3],
                         ["sleep:60", "job:10800", "sleep:86280"])

    def test_retry_keeps_the_daily_anchor(self):
        self.assertEqual(self.scenario(10800, runtime=60, failures=1)[:4],
                         ["job:10800", "sleep:300", "job:11160", "sleep:85980"])

    def test_long_job_skips_missed_windows_without_a_busy_loop(self):
        self.assertEqual(self.scenario(10800, runtime=90000)[:2],
                         ["job:10800", "sleep:82800"])


if __name__ == "__main__":
    (ROOT / "tmp").mkdir(exist_ok=True)
    unittest.main()
