"""2026-10-27 12:20 user decision: stream balance — the board may differ, the stream makes up the totals."""
import copy
import sys
import unittest
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import convert
import levelsb_common as L


class StreamBalance(unittest.TestCase):
    def setUp(self):
        self.a = L.load_a(15)[0]

    def moved(self, k=40):
        """B: k board cells of the most common colour become the second one; the stream gives the totals back."""
        a = self.a
        board, stream = Counter(a['cells']), Counter(a['stream'])
        big = board.most_common(1)[0][0]
        small = next(c for c, _ in board.most_common() if c != big and stream[c] >= k)
        b = copy.deepcopy(a)
        idx = [i for i, c in enumerate(b['cells']) if c == big][:k]
        for i in idx:
            b['cells'][i] = small
        total = Counter(a['cells']) + Counter(a['stream'])
        need = {c: total[c] - Counter(b['cells'])[c] for c in total}
        return b, need, big, small

    def test_rebalance_keeps_length_split_and_totals(self):
        b, need, big, small = self.moved()
        for place in ('even', 'deep'):
            s = convert.rebalance_stream(b['stream'], b['streamOffsets'], need, place)
            self.assertEqual(len(s), len(self.a['stream']))
            self.assertEqual(Counter(s), Counter({c: n for c, n in need.items() if n}))
            b2 = dict(b, stream=s)
            self.assertEqual(L.validate(self.a, b2, 'S', stream_balance=True), [])

    def test_deep_changes_only_the_deepest_beads(self):
        b, need, big, small = self.moved()
        s = convert.rebalance_stream(b['stream'], b['streamOffsets'], need, 'deep')
        off = b['streamOffsets']
        changed = [i for i, (x, y) in enumerate(zip(s, b['stream'])) if x != y]
        for i in changed:
            col = max(c for c in range(len(off) - 1) if off[c] <= i)
            deeper_small = [j for j in range(i + 1, off[col + 1]) if b['stream'][j] == small and s[j] == small]
            self.assertFalse(deeper_small, 'a deeper bead of the same colour was left unchanged')

    def test_validation_rules(self):
        b, need, big, small = self.moved()
        s = convert.rebalance_stream(b['stream'], b['streamOffsets'], need)
        self.assertTrue(L.validate(self.a, dict(b, stream=s), 'S'))                          # flag required
        self.assertTrue(any('总数' in p for p in L.validate(self.a, b, 'S', stream_balance=True)))  # totals must match
        self.assertTrue(any('沙流长度' in p for p in L.validate(self.a, dict(b, stream=s[:-1]), 'S', stream_balance=True)))


class StreamTuneMoves(unittest.TestCase):
    """tune_stream moves: board untouched, colour totals and column lengths kept, queue within the reorder rule."""

    def test_moves_keep_all_invariants(self):
        import random
        import tune_stream as T
        L = T.Level(15)
        rng = random.Random(3)
        stream, perm = L.b['stream'], list(range(len(L.b['queue'])))
        seen = set()
        for _ in range(300):
            p = T.propose(L, stream, perm, rng)
            if not p:
                continue
            kind, s, q = p
            seen.add(kind)
            self.assertEqual(len(s), len(stream))
            self.assertEqual(Counter(s), Counter(stream))
            lv = L.build(s, q)
            self.assertEqual(lv['cells'], L.b['cells'])
            self.assertEqual(lv['streamOffsets'], L.b['streamOffsets'])
            self.assertTrue(L.queue_ok(q))
            self.assertEqual(L.validate_problems(lv), [])
        self.assertEqual(seen, {'seg', 'swap', 'queue'})


if __name__ == '__main__':
    unittest.main()
