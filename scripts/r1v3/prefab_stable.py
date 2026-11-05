#!/usr/bin/env python3
"""Keep the regenerated battle UI prefabs byte-stable when their content did not change.

build.sh deletes and regenerates the six battle UI prefabs on every build
(GardenBattleUiAssets.RecreateForV3). Unity gives every object a new random fileID on each
save, so even an identical layout rewrites about 8000 lines across the six files.

    python3 scripts/r1v3/prefab_stable.py save DIR      before the prefabs are regenerated
    python3 scripts/r1v3/prefab_stable.py restore DIR   after they are regenerated

restore compares the new prefabs with the saved ones object by object, with every local fileID
replaced by the object's path (GameObject names + component type), and nested-prefab references
resolved into the referenced prefab. If all six are the same, the saved bytes are written back, so
git sees no change. If any prefab really changed, all six new files are kept (the page prefab refers
to fileIDs inside the five section prefabs, so they must stay from the same generation) and the
changed objects are listed.

    python3 scripts/r1v3/prefab_stable.py diff DIR      only report, change nothing
"""
import collections
import os
import re
import shutil
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
UI = os.path.join(REPO, 'UnityProject/Assets/Flightbeat/Resources/GardenR1/UI')
NAMES = ['GardenBattlePage', 'GardenBattleBackground', 'GardenBattleHeader',
         'GardenBattleControls', 'GardenPauseDialog', 'GardenResultDialog']
FILES = [n + '.prefab' for n in NAMES]
PREFAB_ASSET_ROOT = '100100000'
REF = re.compile(r'\{fileID: (-?\d+)(?:, guid: (\w+), type: \d+)?\}')


def guids():
    out = {}
    for f in FILES:
        meta = os.path.join(UI, f + '.meta')
        if os.path.exists(meta):
            out[re.search(r'guid: (\w+)', open(meta, encoding='utf-8').read()).group(1)] = f
    return out


def split(text):
    objs = {}
    for part in re.split(r'^--- (?=!u!)', text, flags=re.M)[1:]:
        head, _, body = part.partition('\n')
        m = re.match(r'!u!(\d+) &(-?\d+)( stripped)?', head)
        objs[m.group(2)] = (m.group(1), body, bool(m.group(3)))
    return objs


class Generation:
    """The six prefabs of one generation (a directory), with a stable label for every object."""

    def __init__(self, folder, guid_map):
        self.folder, self.guid = folder, guid_map
        self.objs, self.labels = {}, {}

    def text(self, f):
        return open(os.path.join(self.folder, f), encoding='utf-8').read()

    def parsed(self, f):
        if f not in self.objs:
            self.objs[f] = split(self.text(f))
        return self.objs[f]

    def label(self, f):
        if f in self.labels:
            return self.labels[f]
        objs, L, P = self.parsed(f), {}, {}
        self.labels[f] = L
        for i, (c, b, s) in objs.items():          # objects of a nested prefab: label through that prefab
            if s:
                m = re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+)', b)
                child = self.guid.get(m.group(2))
                L[i] = '[%s]%s' % (child, self.label(child).get(m.group(1), 'MISSING ' + m.group(1)) if child else m.group(2))
            elif c == '1001':
                sp = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+)', b)
                L[i] = 'instance of ' + self.guid.get(sp.group(1), sp.group(1))
        name, comp_go, father, rt_go = {}, {}, {}, {}
        for i, (c, b, s) in objs.items():
            if s:
                continue
            if c == '1':
                name[i] = re.search(r'm_Name: ?(.*)', b).group(1)
            g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
            if g:
                comp_go[i] = g.group(1)
            if c in ('4', '224'):
                rt_go[comp_go[i]] = i
                father[i] = re.search(r'm_Father: \{fileID: (-?\d+)\}', b).group(1)

        def path(go, depth=0):
            if go in P:
                return P[go]
            if go in L:                              # a nested prefab's GameObject
                return L[go]
            parent = father.get(rt_go.get(go), '0')
            p = name.get(go, '?')
            if parent != '0' and depth < 64:
                pgo = comp_go.get(parent)
                p = (path(pgo, depth + 1) if pgo else L.get(parent, '?')) + '/' + p
            P[go] = p
            return p

        seen = collections.Counter()
        for i, (c, b, s) in objs.items():
            if c == '1' and not s:
                p = path(i)
                seen[p] += 1
                L[i] = p + ' (GameObject)' + ('#%d' % seen[p] if seen[p] > 1 else '')
        for i, (c, b, s) in sorted(objs.items()):
            if s or c in ('1', '1001'):
                continue
            go = comp_go.get(i)
            script = re.search(r'm_Script: \{fileID: \d+, guid: (\w+)', b)
            key = '%s <%s%s>' % (path(go) if go else '?', c, ':' + script.group(1)[:8] if script else '')
            seen[key] += 1
            L[i] = key + ('#%d' % seen[key] if seen[key] > 1 else '')
        return L

    def normalized(self, f):
        L, out, broken = self.label(f), {}, []

        def ref(m):
            fid, g = m.group(1), m.group(2)
            if fid == '0':
                return '{none}'
            if g:
                child = self.guid.get(g)
                if not child:
                    return m.group(0)
                if fid == PREFAB_ASSET_ROOT:
                    return '{%s}' % child
                lab = self.label(child).get(fid)
                if lab is None:
                    broken.append('%s -> %s:%s' % (f, child, fid))
                return '{%s:%s}' % (child, lab)
            lab = L.get(fid)
            if lab is None:
                broken.append('%s -> %s' % (f, fid))
            return '{%s}' % lab

        for i, (c, b, s) in self.parsed(f).items():
            body = REF.sub(ref, b)
            body = re.sub(r'^  m_Component:\n(  - component:.*\n)+', '', body, flags=re.M)   # order follows the IDs
            if c == '1001':                          # modification list is sorted by target fileID
                head, sep, rest = body.partition('    m_Modifications:\n')
                if sep:
                    mods = re.split(r'(?=^    - target:)', rest, flags=re.M)
                    tail = mods[-1].split('\n    m_', 1)
                    mods[-1] = tail[0] + ('\n' if len(tail) > 1 else '')
                    body = head + sep + ''.join(sorted(mods)) + ('    m_' + tail[1] if len(tail) > 1 else '')
            key = L.get(i, i)
            while key in out:
                key += '*'
            out[key] = body
        return out, broken


def compare(before_dir, after_dir):
    g = guids()
    old, new = Generation(before_dir, g), Generation(after_dir, g)
    changes, broken = [], []
    for f in FILES:
        a, _ = old.normalized(f)
        b, bad = new.normalized(f)
        broken += bad
        for k in sorted(set(a) | set(b)):
            if k not in b:
                changes.append('%s: 删除 %s' % (f, k))
            elif k not in a:
                changes.append('%s: 新增 %s' % (f, k))
            elif a[k] != b[k]:
                changes.append('%s: 修改 %s' % (f, k))
    return changes, broken


def main():
    if len(sys.argv) != 3 or sys.argv[1] not in ('save', 'restore', 'diff'):
        sys.exit(__doc__)
    cmd, snap = sys.argv[1], sys.argv[2]
    if cmd == 'save':
        os.makedirs(snap, exist_ok=True)
        for f in FILES:
            if os.path.exists(os.path.join(UI, f)):
                shutil.copy2(os.path.join(UI, f), os.path.join(snap, f))
        return
    if not all(os.path.exists(os.path.join(p, f)) for p in (snap, UI) for f in FILES):
        print('界面 Prefab：没有完整的上一版可比，保留新生成的文件')
        return
    if all(open(os.path.join(snap, f), 'rb').read() == open(os.path.join(UI, f), 'rb').read() for f in FILES):
        print('界面 Prefab：文件未变')
        return
    changes, broken = compare(snap, UI)
    if broken:
        print('界面 Prefab：新生成的文件里有 %d 处引用找不到对象：' % len(broken))
        for b in broken[:20]:
            print('  ' + b)
        sys.exit(1)
    if not changes:
        if cmd == 'restore':
            for f in FILES:
                shutil.copyfile(os.path.join(snap, f), os.path.join(UI, f))
            print('界面 Prefab：内容与上一版相同（只是 Unity 重新编号），已保留原文件，git 不会出现改动')
        else:
            print('界面 Prefab：内容相同，只是编号不同')
        return
    print('界面 Prefab：内容有 %d 处变化，保留新生成的文件：' % len(changes))
    for c in changes[:40]:
        print('  ' + c)
    if len(changes) > 40:
        print('  …… 另有 %d 处' % (len(changes) - 40))


if __name__ == '__main__':
    main()
