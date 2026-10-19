"""Release guard regressions. All resources/manifests live in a temporary directory."""
import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import levelsb_common as lc
import publish


class ReleaseChecks(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='levels-b-release-checks-')
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.b = self.root / 'levels/B'
        self.b.mkdir(parents=True)
        level, raw = lc.load_a(3)
        self.entry = dict(game=3, resource='sector01', fileSha256=lc.file_sha256(raw),
                          cellsSha256=lc.cells_sha256(level['cells']))
        (self.b / 'sector01.json').write_bytes(raw)
        (self.b / 'released.json').write_text(json.dumps({'levels': [self.entry]}))
        for name in ('sector01.json.meta', 'released.json.meta'):
            (self.b / name).write_text('fileFormatVersion: 2\n')
        self.addCleanup(patch.stopall)
        for name, value in dict(ROOT=self.root, B_DIR=self.b, TEST_DIR=self.root / 'levels/B-test',
                                RELEASED=self.b / 'released.json').items():
            patch.object(publish, name, value).start()
        patch.object(publish, 'load_manifest', return_value={'levels': {
            'sector01': dict(self.entry, status='approved', mode='S')}}).start()

    def check(self, allow_test=False):
        with contextlib.redirect_stdout(io.StringIO()):
            return publish.check(allow_test)

    def test_approved_files_and_their_meta_pass(self):
        self.assertEqual(self.check(), 0)

    def test_extra_resources_and_orphan_meta_fail(self):
        for name in ('sector02.json', 'draft/sector02.json', 'sector01.txt', 'draft/raw.bytes',
                     'orphan.json.meta', 'draft/sector01.json.meta'):
            with self.subTest(name=name):
                file = self.b / name
                file.parent.mkdir(parents=True, exist_ok=True)
                file.write_text('{}')
                self.assertEqual(self.check(), 1)
                file.unlink()

    def test_symlink_cannot_hide_resources(self):
        external = self.root / 'external'
        external.mkdir()
        (external / 'unapproved.json').write_text('{}')
        (self.b / 'linked').symlink_to(external, target_is_directory=True)
        self.assertEqual(self.check(), 1)

    def test_changed_file_and_missing_file_fail(self):
        file = self.b / 'sector01.json'
        level = json.loads(file.read_bytes())
        level['title'] += 'changed'
        file.write_text(json.dumps(level))
        self.assertEqual(self.check(), 1)
        file.unlink()
        self.assertEqual(self.check(), 1)

    def test_unapproved_status_fails(self):
        with patch.object(publish, 'load_manifest', return_value={'levels': {
                'sector01': dict(self.entry, status='draft', mode='S')}}):
            self.assertEqual(self.check(), 1)

    def test_test_channel_requires_explicit_build_flag(self):
        publish.TEST_DIR.mkdir()
        self.assertEqual(self.check(), 1)
        self.assertEqual(self.check(allow_test=True), 0)


if __name__ == '__main__':
    unittest.main()
