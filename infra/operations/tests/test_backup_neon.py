import datetime as dt
import importlib.util
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location("backup_neon", Path(__file__).parents[1] / "backup-neon.py")
backup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(backup)


class RetentionTests(unittest.TestCase):
    def test_retains_exact_thirty_day_boundary(self):
        now = dt.datetime(2026, 11, 10, 3, tzinfo=dt.timezone.utc)
        receipt = {"job": backup.JOB, "status": "success"}
        self.assertFalse(backup.expired_run("20261011T030000Z-1234abcd", receipt, now))
        self.assertTrue(backup.expired_run("20261011T025959Z-1234abcd", receipt, now))

    def test_ignores_unrelated_names_and_incomplete_or_other_jobs(self):
        now = dt.datetime(2026, 11, 10, tzinfo=dt.timezone.utc)
        for name in ["20261301T000000Z-1234abcd", "important", "20260101T000000Z-1234abcd.old", "../20260101T000000Z-1234abcd"]:
            self.assertFalse(backup.expired_run(name, {"job": backup.JOB, "status": "success"}, now))
        for receipt in [{}, {"job": "other", "status": "success"}, {"job": backup.JOB, "status": "failed"}, {"job": backup.JOB, "status": "running"}]:
            self.assertFalse(backup.expired_run("20260101T000000Z-1234abcd", receipt, now))


if __name__ == "__main__":
    unittest.main()
