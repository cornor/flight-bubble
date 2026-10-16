using System;
using System.Collections.Generic;

namespace Flightbeat.Model
{
    // TEngine.MemoryPool's source algorithms, kept in the independent model.
    public static partial class MemoryPool
    {
        // f54941: dictionary, list, then false strict flag.
        private static readonly Dictionary<Type, MemoryCollection> _memoryCollections = new Dictionary<Type, MemoryCollection>();
        private static readonly List<MemoryCollection> _memoryCollectionList = new List<MemoryCollection>();
        private static bool _enableStrictCheck;
        // f54921/f54922/f54923.
        public static bool EnableStrictCheck { get => _enableStrictCheck; set => _enableStrictCheck = value; }
        public static int Count => _memoryCollections.Count;
        // f55350 does not call InternalCheckMemoryType; the non-generic f54929 does.
        public static T Acquire<T>() => GetMemoryCollection(typeof(T)).Acquire<T>();
        public static IMemory Acquire(Type memoryType)
        { InternalCheckMemoryType(memoryType); return GetMemoryCollection(memoryType).Acquire(); }
        // f54933: null fails before type/collection checks.
        public static void Release(IMemory memory)
        {
            if (memory == null) throw new Exception("Memory is invalid.");
            Type type = memory.GetType();
            InternalCheckMemoryType(type);
            GetMemoryCollection(type).Release(memory);
        }
        // f54930.
        private static void InternalCheckMemoryType(Type memoryType)
        {
            if (!_enableStrictCheck) return;
            if (memoryType == null) throw new Exception("Memory type is invalid.");
            if (!memoryType.IsClass || memoryType.IsAbstract) throw new Exception("Memory type is not a non-abstract class type.");
            // f54930's class+400 virtual slot 26 is RuntimeType.get_FullName
            // (Reverse/dump/il2cpp.h, System_RuntimeType_VTable).
            if (!typeof(IMemory).IsAssignableFrom(memoryType)) throw new Exception(string.Format("Memory type '{0}' is invalid.", memoryType.FullName));
        }
        // f54931. The dictionary lock protects both registry publications.
        private static MemoryCollection GetMemoryCollection(Type memoryType)
        {
            if (memoryType == null) throw new Exception("MemoryType is invalid.");
            lock (_memoryCollections)
            {
                if (_memoryCollections.TryGetValue(memoryType, out MemoryCollection collection)) return collection;
                collection = new MemoryCollection(memoryType);
                _memoryCollections.Add(memoryType, collection);
                _memoryCollectionList.Add(collection);
                return collection;
            }
        }
        private sealed partial class MemoryCollection
        {
            private readonly Queue<IMemory> _memories;
            private readonly Type _memoryType;
            private int _usingMemoryCount, _acquireMemoryCount, _releaseMemoryCount, _addMemoryCount, _removeMemoryCount;
            // f54940. These counters are observable source state, not diagnostics
            // added to the candidate; increments/decrements use i32 wrapping.
            internal MemoryCollection(Type type)
            { _memories = new Queue<IMemory>(); _memoryType = type; _usingMemoryCount = _acquireMemoryCount = _releaseMemoryCount = _addMemoryCount = _removeMemoryCount = 0; }
            // f28590: type check precedes counters; newly allocated values are not Clear'ed.
            internal T Acquire<T>()
            {
                if (typeof(T) != _memoryType) throw new Exception("Type is invalid.");
                _usingMemoryCount = unchecked(_usingMemoryCount + 1);
                _acquireMemoryCount = unchecked(_acquireMemoryCount + 1);
                lock (_memories) { if (_memories.Count > 0) return (T)(object)_memories.Dequeue(); }
                _addMemoryCount = unchecked(_addMemoryCount + 1);
                return Activator.CreateInstance<T>();
            }
            // f54932: same queue order, untyped allocation and IMemory cast.
            internal IMemory Acquire()
            {
                _usingMemoryCount = unchecked(_usingMemoryCount + 1);
                _acquireMemoryCount = unchecked(_acquireMemoryCount + 1);
                lock (_memories) { if (_memories.Count > 0) return _memories.Dequeue(); }
                _addMemoryCount = unchecked(_addMemoryCount + 1);
                return CreateMemory(_memoryType); // original f2401/f1364 cast error text
            }
            // f54934: Clear runs BEFORE strict duplicate detection. Queue is FIFO;
            // permissive mode admits duplicate/foreign releases and negative usage.
            internal void Release(IMemory memory)
            {
                memory.Clear();
                lock (_memories)
                {
                    if (_enableStrictCheck && _memories.Contains(memory)) throw new Exception("The memory has been released.");
                    _memories.Enqueue(memory);
                }
                _releaseMemoryCount = unchecked(_releaseMemoryCount + 1);
                _usingMemoryCount = unchecked(_usingMemoryCount - 1);
            }
        }
    }
    public sealed partial class Battle
    {
        private static Shot AcquireShot() => MemoryPool.Acquire<Shot>(); // f55350
        private static void ReleaseShot(Shot shot) => MemoryPool.Release(shot); // f54933
    }
}
