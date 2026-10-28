#!/usr/bin/env python3
"""Regression cases for false candidate rejection, crop loss, protected appendages and exact refill counts."""
import json
import tempfile
import unittest
from pathlib import Path

import numpy as np

import convert
from conversion_guards import (candidate_key, crop_safe, foreground_bounds, load_constraints,
                               protection_mask, refill_budget)
from levelsb_common import file_sha256


class ConversionGuards(unittest.TestCase):
    def test_all_gates_precede_ranking(self):
        def candidate(changes, budget_ok, mix_ok=True):
            return dict(changes=changes, budget=dict(withinLimits=budget_ok, colors=[dict(over=not budget_ok, moved=10 if budget_ok else 11, limit=10)]),
                        passes=mix_ok, mixExact=.1, transform=dict(scale=1, dx=0, dy=0, mirror=False))
        invalid = candidate(140, False)
        valid = candidate(149, True)
        self.assertIs(sorted([invalid, valid], key=candidate_key)[0], valid)
        self.assertIs(sorted([candidate(151, False), valid], key=candidate_key)[0], valid)
        # 2026-10-26 23:31 (user): D10/D20 is reported only, so it no longer outranks the change count.
        fewer = candidate(0, True, False)
        self.assertIs(sorted([fewer, valid], key=candidate_key)[0], fewer)

    def test_budget_counts_both_ends_and_exact_boundaries(self):
        grid = np.zeros((60, 60), int); final = grid.copy()
        final.flat[:10] = 1
        budget = refill_budget(grid, final, {0: 3590, 1: 10})
        self.assertTrue(budget['withinLimits'])
        self.assertEqual([c['moved'] for c in budget['colors']], [10, 10])
        final.flat[10] = 1
        self.assertFalse(refill_budget(grid, final, {0: 3589, 1: 11})['withinLimits'])
        final.flat[:151] = 1
        self.assertFalse(refill_budget(grid, final, {0: 1800, 1: 1800})['withinLimits'])

    def test_crop_edges_and_mirror(self):
        bounds = (2, 10, 45, 58)
        self.assertTrue(crop_safe(bounds))
        self.assertFalse(crop_safe(bounds, 1.1, -3, -3))
        self.assertTrue(crop_safe(bounds, 1, 0, 0, True))
        self.assertFalse(crop_safe(bounds, 1, 0, 3))

    def test_scenery_vs_explicit_subject(self):
        pick = np.zeros((600, 600), int)
        pick[590:, :] = 1
        pick[100:500, 100:400] = 1
        self.assertFalse(crop_safe(foreground_bounds(pick, 0), 1.1))
        subject = foreground_bounds(pick, 0, {'subjectRects': [[9, 9, 41, 51]]})
        self.assertTrue(crop_safe(subject, 1.1))
        with self.assertRaises(ValueError):
            foreground_bounds(pick, 0, {'subjectRects': [[0, 0, 1, 1]]})

    def test_regions_follow_translation_mirror_and_editable_mask(self):
        cfg = {'protectRects': [[10, 20, 12, 22]], 'editableRects': [[0, 0, 30, 60]]}
        mask = protection_mask(cfg)
        self.assertTrue(mask[20, 10]); self.assertFalse(mask[20, 9]); self.assertTrue(mask[20, 40])
        shifted = protection_mask({'protectRects': cfg['protectRects']}, dx=3, dy=-3, mirror=True)
        self.assertTrue(shifted[17, 51]); self.assertFalse(shifted[20, 10])

    def test_source_hash_binding(self):
        with tempfile.TemporaryDirectory() as folder:
            image, config = Path(folder) / 'image', Path(folder) / 'config.json'
            image.write_bytes(b'original')
            config.write_text(json.dumps({'sourceSha256': file_sha256(b'original'), 'protectRects': [[1, 2, 3, 4]]}))
            self.assertIn('sha256', load_constraints(config, image))
            image.write_bytes(b'different')
            with self.assertRaises(ValueError):
                load_constraints(config, image)

    def test_refill_preserves_protected_appendage_and_exact_counts(self):
        grid = np.zeros((60, 60), int)
        grid[15:40, 20:45] = 1
        grid[30:33, 8:20] = 1  # left arm
        protected = np.zeros_like(grid, bool); protected[28:35, 6:22] = True
        target = {0: int((grid == 0).sum()) + 40, 1: int((grid == 1).sum()) - 40}
        result = convert.refill(grid, target, np.zeros((60, 60, 2)), [0, 1], protected)
        np.testing.assert_array_equal(result[protected], grid[protected])
        self.assertEqual({c: int((result == c).sum()) for c in target}, target)

    def test_infeasible_protection_fails_instead_of_editing(self):
        grid = np.zeros((60, 60), int); grid[15:40, 20:45] = 1
        with self.assertRaises(convert.RefillBlocked):
            convert.refill(grid, {0: 3000, 1: 600}, np.zeros((60, 60, 2)), [0, 1], np.ones_like(grid, bool))

    def test_small_components_are_hard_protected(self):
        grid = np.zeros((60, 60), int); grid[10:13, 10:13] = 1
        with self.assertRaises(convert.RefillBlocked):
            convert.refill(grid, {0: 3592, 1: 8}, np.zeros((60, 60, 2)), [0, 1])

    def test_fill_color_cannot_spill_into_background(self):
        grid = np.zeros((60, 60), int)
        grid[10:40, 10:40] = 1
        grid[15:25, 15:25] = 2
        target = {0: int((grid == 0).sum()) - 10, 1: int((grid == 1).sum()) - 10, 2: 120}
        result = convert.refill(grid, target, np.zeros((60, 60, 3)), [0, 1, 2], recolor_from={'2': [1]})
        self.assertFalse(((result == 2) & (grid == 0)).any())
        self.assertEqual(int((result == 2).sum()), 120)

    def test_refill_uses_other_boundary_when_intermediate_colour_budget_runs_out(self):
        # The thin ring (1) is the first/shortest route from background (0) to centre (2).
        # It cannot carry all ten transfers within its own 15% edit cap. The wide bridge (3) can.
        grid = np.zeros((60, 60), int)
        grid[15:45, 15:45] = 1
        grid[16:44, 16:44] = 2
        grid[30:40, 10:20] = 3
        target = {c: int((grid == c).sum()) for c in range(4)}
        target[0] -= 10; target[2] += 10
        distance = np.zeros((60, 60, 4))
        rules = {'2': [1, 3]}  # The central feature must not expand directly into original background.
        legacy = convert.refill(grid, target, distance, list(range(4)), recolor_from=rules)
        self.assertFalse(refill_budget(grid, legacy, target)['withinLimits'])
        final, retried = convert.refill_candidate(grid, target, distance, list(range(4)), recolor_from=rules)
        self.assertTrue(retried)
        self.assertTrue(refill_budget(grid, final, target)['withinLimits'])
        self.assertEqual({c: int((final == c).sum()) for c in target}, target)

    def test_budget_retry_keeps_existing_passing_result_and_rejects_impossible_budget(self):
        grid = np.zeros((60, 60), int); grid[15:40, 20:45] = 1
        distance = np.zeros((60, 60, 2))
        target = {0: 2985, 1: 615}
        legacy = convert.refill(grid, target, distance, [0, 1])
        final, retried = convert.refill_candidate(grid, target, distance, [0, 1])
        self.assertFalse(retried)
        np.testing.assert_array_equal(final, legacy)
        with self.assertRaises(convert.RefillBlocked):
            convert.refill(grid, {0: 3175, 1: 425}, distance, [0, 1], enforce_budget=True)


if __name__ == '__main__':
    unittest.main()
