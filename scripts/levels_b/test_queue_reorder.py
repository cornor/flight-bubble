"""2026-10-26 user decision: B may reorder the queue (same members and ammo); everything else stays exact."""
import copy
import json
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import levelsb_common as L


def level(game=25):
    return L.load_a(game)[0]


class QueueReorder(unittest.TestCase):
    def swap(self, q, i, j):
        keep = {k: (q[i].get(k), q[j].get(k)) for k in L.QUEUE_SLOT_FIELDS if k in q[i]}
        q[i], q[j] = q[j], q[i]
        for k, (vi, vj) in keep.items():
            q[i][k], q[j][k] = vi, vj

    def test_identical_queue_is_valid(self):
        a = level()
        self.assertEqual(L.validate(a, copy.deepcopy(a), 'S'), [])

    def test_member_swap_within_limit_is_valid(self):
        a = level(); b = copy.deepcopy(a); q = b['queue']
        lanes = a['laneCount']
        j = next(j for j in range(1, 3 * lanes) if q[j]['color'] != q[0]['color'])
        self.swap(q, 0, j)
        self.assertEqual(L.validate(a, b, 'S'), [])
        problems, stats = L.queue_reorder(a['queue'], b['queue'])
        self.assertEqual(stats['moved'], 2)
        self.assertLessEqual(stats['maxRowShift'], 2)

    def test_far_move_is_rejected(self):
        a = level(); b = copy.deepcopy(a); q = b['queue']
        j = next(j for j in range(len(q) - 1, 0, -1) if q[j]['color'] != q[0]['color'])
        self.swap(q, 0, j)
        self.assertTrue(any('幅度过大' in p for p in L.validate(a, b, 'S')))

    def test_changed_ammo_or_slot_is_rejected(self):
        a = level(); b = copy.deepcopy(a); b['queue'][0]['ammo'] += 1; b['queue'][1]['ammo'] -= 1
        self.assertTrue(any('队员或弹量' in p for p in L.validate(a, b, 'S')))
        b = copy.deepcopy(a); b['queue'][0]['lane'], b['queue'][1]['lane'] = b['queue'][1]['lane'], b['queue'][0]['lane']
        self.assertTrue(any('排队位置' in p for p in L.validate(a, b, 'S')))

    def test_special_member_must_stay(self):
        for g in range(3, 263):
            a = level(g)
            idx = [i for i, e in enumerate(a['queue']) if e.get('kind', 0) != 0]
            if idx:
                break
        b = copy.deepcopy(a); q = b['queue']; i = idx[0]
        j = next(j for j in range(max(0, i - 4), min(len(q), i + 5)) if L._queue_member(q[j]) != L._queue_member(q[i]))
        self.swap(q, i, j)
        self.assertTrue(any('特殊队员' in p for p in L.validate(a, b, 'S')))


class Recolor(unittest.TestCase):
    """2026-10-26 23:31 user decision: mode R may map colours onto other members that have already debuted."""

    def test_recolor_to_debuted_member_is_valid(self):
        g = 20
        a = level(g)
        used = set(L.level_colors(a)); allowed = L.allowed_colors(g, a); debut = L.debut_colors(g, a)
        src = next(c for c in sorted(used) if c not in debut)
        dst = next(c for c in sorted(allowed) if c not in used)
        b = L.apply_sigma(a, {src: dst})
        self.assertEqual(L.validate(a, b, 'R', {src: dst}, g), [])
        self.assertTrue(any('置换' in p for p in L.validate(a, b, 'P', {src: dst}, g)))   # P stays a permutation

    def test_recolor_rejects_undebuted_member_and_lost_debut(self):
        g = 7
        a = level(g)
        used = set(L.level_colors(a)); allowed = L.allowed_colors(g, a); debut = L.debut_colors(g, a)
        palette = set(range(len(a['palette']) // 3))
        early = next(c for c in sorted(palette - allowed - used))
        src = next(c for c in sorted(used) if c not in debut)
        self.assertTrue(any('还没登场' in p for p in L.validate(a, L.apply_sigma(a, {src: early}), 'R', {src: early}, g)))
        d = sorted(debut)[0]
        spare = next(c for c in sorted(allowed) if c not in used)
        self.assertTrue(any('新角色' in p for p in L.validate(a, L.apply_sigma(a, {d: spare}), 'R', {d: spare}, g)))

    def test_recolor_must_be_one_to_one(self):
        g = 20
        a = level(g)
        cs = sorted(L.level_colors(a))
        self.assertTrue(any('一一对应' in p for p in L.validate(a, copy.deepcopy(a), 'R', {cs[0]: cs[1]}, g)))


if __name__ == '__main__':
    unittest.main()
