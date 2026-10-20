"""Simulated runner regression tests. Real Unity observations are separate evidence, not these fixtures."""
import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

import evaluate
import levelsb_common as lc


class EvaluationDiagnostics(unittest.TestCase):
    def run_fixture(self, extra=(), exit_code=0, passed=True):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            a = root / 'levels'
            a.mkdir()
            _, raw = lc.load_a(3)
            (a / 'sector01.json').write_bytes(raw)
            manifest = {'levels': {'sector01': {'draft': str(a / 'sector01.json'),
                                                'fileSha256': lc.file_sha256(raw)}}}
            captured = []

            def fake_unity(args, **kwargs):
                inp = Path(args[args.index('--levels-b-eval-input') + 1])
                out = Path(args[args.index('--levels-b-eval-output') + 1])
                cases = json.loads(inp.read_text())['cases']
                captured.extend(cases)
                results = [dict(name=c['name'], path=c['path'], game=c['game'], strategy=c['strategy'], seed=c['seed'],
                                outcome='won', revived=False, seconds=10., blockedSeconds=0., minDistance=20., progress=1.)
                           for c in cases]
                out.write_text(json.dumps(dict(passed=passed, results=results)))
                return SimpleNamespace(returncode=exit_code)

            argv = ['evaluate.py', '3', '--runs', '1', '--strategies', '0', '--out', str(root / 'out'), *extra]
            with patch.object(evaluate, 'ROOT', root), patch.object(evaluate, 'LEVELS_A', a), \
                    patch.object(evaluate, 'load_manifest', return_value=manifest), \
                    patch.object(evaluate, 'record_check') as record, \
                    patch.object(evaluate.subprocess, 'run', side_effect=fake_unity), \
                    patch('sys.argv', argv), contextlib.redirect_stdout(io.StringIO()):
                status = evaluate.main()
                return status, record.call_count, captured

    def test_no_manifest_runs_comparison_without_recording_status(self):
        status, records, cases = self.run_fixture(['--no-manifest'])
        self.assertEqual((status, records), (0, 0))
        self.assertEqual([c['seed'] for c in cases], [1, 1])

    def test_trace_flag_preserves_case_identity_and_seeds(self):
        _, _, ordinary = self.run_fixture(['--no-manifest'])
        status, records, traced = self.run_fixture(['--no-manifest', '--trace'])
        self.assertEqual((status, records), (0, 0))
        self.assertTrue(all(c['trace'] for c in traced))
        self.assertEqual([{k: v for k, v in c.items() if k not in ('trace', 'path')} for c in ordinary],
                         [{k: v for k, v in c.items() if k not in ('trace', 'path')} for c in traced])

    def test_failed_unity_exit_never_marks_evaluated(self):
        status, records, _ = self.run_fixture(exit_code=1)
        self.assertEqual((status, records), (1, 0))

    def test_failed_report_never_marks_evaluated(self):
        status, records, _ = self.run_fixture(passed=False)
        self.assertEqual((status, records), (1, 0))

    def test_independent_candidates_cannot_update_manifest(self):
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit) as failure:
            self.run_fixture(['--candidate-dir', '/unused'])
        self.assertEqual(failure.exception.code, 2)


if __name__ == '__main__':
    unittest.main()
