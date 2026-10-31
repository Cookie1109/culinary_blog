"""Negative controls: unrelated assemblies and rounded rates cannot pass the gate."""

import pathlib
import subprocess
import sys
import tempfile
import unittest


class CoverageGateTests(unittest.TestCase):
    def run_gate(self, covered, total, name="CulinaryBlog.Application"):
        lines = "".join(f'<line number="{i}" hits="{int(i < covered)}"/>' for i in range(total))
        with tempfile.TemporaryDirectory() as directory:
            report = pathlib.Path(directory) / "coverage.xml"
            report.write_text(
                f'<coverage><packages><package name="{name}" line-rate="1" branch-rate="0.5">'
                f'<classes><class filename="request.cs"><lines>{lines}</lines></class></classes>'
                '</package><package name="CulinaryBlog.Domain" line-rate="1"/></packages></coverage>',
                encoding="utf-8",
            )
            return subprocess.run(
                [sys.executable, str(pathlib.Path(__file__).with_name("check-application-coverage.py")), str(report)],
                capture_output=True, text=True, check=False,
            ).returncode

    def test_threshold(self):
        self.assertEqual(0, self.run_gate(80, 100))
        self.assertEqual(1, self.run_gate(79, 100))
        self.assertEqual(1, self.run_gate(7999, 10000))

    def test_missing_or_empty_application(self):
        self.assertEqual(2, self.run_gate(100, 100, "Other"))
        self.assertEqual(2, self.run_gate(0, 0))

    def test_results_directory_ignores_trx_attachment_copy_but_rejects_multiple_runs(self):
        xml = ('<coverage><packages><package name="CulinaryBlog.Application">'
               '<classes><class filename="request.cs"><lines><line number="1" hits="1"/>'
               '</lines></class></classes></package></packages></coverage>')
        with tempfile.TemporaryDirectory() as directory:
            results = pathlib.Path(directory)
            canonical = results / "run-one"
            canonical.mkdir()
            (canonical / "coverage.cobertura.xml").write_text(xml, encoding="utf-8")
            copied = results / "test-run" / "In" / "host"
            copied.mkdir(parents=True)
            (copied / "coverage.cobertura.xml").write_text(xml, encoding="utf-8")
            command = [sys.executable, str(pathlib.Path(__file__).with_name("check-application-coverage.py")), directory]
            self.assertEqual(0, subprocess.run(command, capture_output=True, check=False).returncode)
            second = results / "run-two"
            second.mkdir()
            (second / "coverage.cobertura.xml").write_text(xml, encoding="utf-8")
            self.assertEqual(2, subprocess.run(command, capture_output=True, check=False).returncode)


if __name__ == "__main__":
    unittest.main()
