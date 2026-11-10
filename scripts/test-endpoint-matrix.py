"""Negative controls for runtime inventory and complete passing theory evidence."""

import copy
from pathlib import Path
import runpy
import tempfile
import unittest

verify = runpy.run_path(str(Path(__file__).with_name("verify-endpoint-matrix.py")))["verify"]
SYMBOL = "AdminUsersApiTests.DirectoryRejectsInvalidQueriesWithValidationProblem"


class MatrixGateTests(unittest.TestCase):
    def setUp(self):
        self.matrix = {"operations": [{"operation": "GET /api/v1/admin/users", "cases": {
            group: [SYMBOL] for group in ("happy", "error", "authorization")}}]}
        self.contract = {"paths": {"/api/v1/admin/users": {"get": {}}}}

    def check(self, outcomes, matrix=None, contract=None):
        with tempfile.TemporaryDirectory() as directory:
            trx = Path(directory) / "test.trx"
            trx.write_text("<TestRun>" + "".join(
                f'<UnitTestResult testName="CulinaryBlog.IntegrationTests.{SYMBOL}(case: {index})" outcome="{outcome}"/>'
                for index, outcome in enumerate(outcomes)) + "</TestRun>", encoding="utf-8")
            return verify(matrix or self.matrix, self.contract if contract is None else contract, trx)

    def test_passed_theories(self):
        self.assertEqual(1, self.check(["Passed", "Passed"])["operations"])

    def test_any_failed_skipped_or_missing_case_rejects_evidence(self):
        for outcomes in (["Passed", "Failed"], ["Passed", "NotExecuted"], []):
            with self.subTest(outcomes=outcomes), self.assertRaises(ValueError):
                self.check(outcomes)

    def test_absent_runtime_route_and_duplicate_row_are_rejected(self):
        with self.assertRaises(ValueError):
            self.check(["Passed"], contract={"paths": {}})
        matrix = copy.deepcopy(self.matrix)
        matrix["operations"] *= 2
        with self.assertRaises(ValueError):
            self.check(["Passed"], matrix=matrix)

    def test_missing_authorization_or_source_symbol_is_rejected(self):
        for group in ([], ["AdminUsersApiTests.NoSuchTest"]):
            matrix = copy.deepcopy(self.matrix)
            matrix["operations"][0]["cases"]["authorization"] = group
            with self.subTest(group=group), self.assertRaises(ValueError):
                self.check(["Passed"], matrix=matrix)

    def test_unmapped_runtime_operation_is_rejected(self):
        contract = copy.deepcopy(self.contract)
        contract["paths"]["/api/v1/release"] = {"get": {}}
        with self.assertRaisesRegex(ValueError, "Runtime operations missing"):
            self.check(["Passed"], contract=contract)


if __name__ == "__main__":
    unittest.main()
