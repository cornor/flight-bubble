using System;

namespace Flightbeat.Model
{
    public static partial class MemoryPool
    {
        // f54924: snapshot registry-list order under the registry lock. The
        // source reads each queue count without acquiring a second queue lock.
        public static MemoryPoolInfo[] GetAllMemoryPoolInfos()
        {
            lock (_memoryCollections)
            {
                var result = new MemoryPoolInfo[_memoryCollectionList.Count];
                for (int i = 0; i < _memoryCollectionList.Count; i++)
                {
                    MemoryCollection c = _memoryCollectionList[i];
                    result[i] = new MemoryPoolInfo(c.MemoryType, c.UnusedMemoryCount, c.UsingMemoryCount,
                        c.AcquireMemoryCount, c.ReleaseMemoryCount, c.AddMemoryCount, c.RemoveMemoryCount);
                }
                return result;
            }
        }
        // f54927: RemoveAll each collection before discarding the registry.
        // Outstanding borrowed objects remain alive and may be released later.
        public static void ClearAll()
        {
            lock (_memoryCollections)
            {
                for (int i = 0; i < _memoryCollectionList.Count; i++) _memoryCollectionList[i].RemoveAll();
                _memoryCollections.Clear();
                _memoryCollectionList.Clear();
            }
        }
        // f55351/55352/55353: generic wrappers bypass strict type validation.
        public static void Add<T>(int count) => GetMemoryCollection(typeof(T)).Add<T>(count);
        public static void Remove<T>(int count) => GetMemoryCollection(typeof(T)).Remove(count);
        public static void RemoveAll<T>() => GetMemoryCollection(typeof(T)).RemoveAll();
        // f54935/54937/54939: untyped wrappers validate before registration.
        public static void Add(Type type, int count)
        { InternalCheckMemoryType(type); GetMemoryCollection(type).Add(count); }
        public static void Remove(Type type, int count)
        { InternalCheckMemoryType(type); GetMemoryCollection(type).Remove(count); }
        public static void RemoveAll(Type type)
        { InternalCheckMemoryType(type); GetMemoryCollection(type).RemoveAll(); }

        // f54936/f54932 -> table[851] f2401 -> f1364. IL2CPP constructs
        // this cast error from class element names and source UTF-8 fragments
        // at 457836 / 457776 / 371370; preserve it across the CLR boundary.
        private static IMemory CreateMemory(Type type)
        {
            object value = Activator.CreateInstance(type);
            if (value == null || value is IMemory) return (IMemory)value;
            Type source = value.GetType();
            throw new InvalidCastException("Unable to cast object of type '" + (source.HasElementType ? source.GetElementType().Name : source.Name) + "' to type 'IMemory'.");
        }

        private sealed partial class MemoryCollection
        {
            public Type MemoryType => _memoryType; // f3234
            public int UnusedMemoryCount => _memories.Count; // f54925
            public int UsingMemoryCount => _usingMemoryCount; // f4596
            public int AcquireMemoryCount => _acquireMemoryCount; // f3235
            public int ReleaseMemoryCount => _releaseMemoryCount; // f6993
            public int AddMemoryCount => _addMemoryCount; // f7354
            public int RemoveMemoryCount => _removeMemoryCount; // f7113
            // f28591. Type mismatch precedes locking/counter mutation. Negative
            // counts still alter the counter; constructors do not call Clear.
            internal void Add<T>(int count)
            {
                if (typeof(T) != _memoryType) throw new Exception("Type is invalid.");
                lock (_memories)
                {
                    _addMemoryCount = unchecked(_addMemoryCount + count);
                    while (count > 0) { _memories.Enqueue((IMemory)(object)Activator.CreateInstance<T>()); count--; }
                }
            }
            // f54936. The counter is charged in full before any allocation;
            // an allocation/cast exception preserves already enqueued objects.
            internal void Add(int count)
            {
                lock (_memories)
                {
                    _addMemoryCount = unchecked(_addMemoryCount + count);
                    while (count > 0) { _memories.Enqueue(CreateMemory(_memoryType)); count--; }
                }
            }
            // f54938: signed min with current unused count, including negatives.
            internal void Remove(int count)
            {
                lock (_memories)
                {
                    count = count > _memories.Count ? _memories.Count : count;
                    _removeMemoryCount = unchecked(_removeMemoryCount + count);
                    while (count > 0) { _memories.Dequeue(); count--; }
                }
            }
            // f54928: removal counter update occurs before Queue.Clear.
            internal void RemoveAll()
            {
                lock (_memories)
                {
                    _removeMemoryCount = unchecked(_removeMemoryCount + _memories.Count);
                    _memories.Clear();
                }
            }
        }
    }

    public struct MemoryPoolInfo
    {
        private readonly Type _type;
        private readonly int _unusedMemoryCount, _usingMemoryCount, _acquireMemoryCount, _releaseMemoryCount, _addMemoryCount, _removeMemoryCount;
        // f54926: copied values, not a live view of subsequent pool operations.
        public MemoryPoolInfo(Type type, int unusedMemoryCount, int usingMemoryCount, int acquireMemoryCount,
            int releaseMemoryCount, int addMemoryCount, int removeMemoryCount)
        {
            _type = type; _unusedMemoryCount = unusedMemoryCount; _usingMemoryCount = usingMemoryCount;
            _acquireMemoryCount = acquireMemoryCount; _releaseMemoryCount = releaseMemoryCount;
            _addMemoryCount = addMemoryCount; _removeMemoryCount = removeMemoryCount;
        }
        public Type Type => _type; // f5219
        public int UnusedMemoryCount => _unusedMemoryCount; // f5221
        public int UsingMemoryCount => _usingMemoryCount; // f5220
        public int AcquireMemoryCount => _acquireMemoryCount; // f3234
        public int ReleaseMemoryCount => _releaseMemoryCount; // f4596
        public int AddMemoryCount => _addMemoryCount; // f3235
        public int RemoveMemoryCount => _removeMemoryCount; // f6993
    }
}
