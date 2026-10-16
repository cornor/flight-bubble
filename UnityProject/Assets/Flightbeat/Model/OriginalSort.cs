using System;
using System.Collections.Generic;

namespace Flightbeat.Model
{
    // Original int and Vector2Int ArraySortHelper bodies use the same algorithm.
    // Preserve their equal-key permutations rather than a platform List.Sort.
    internal static class OriginalSort
    {
        // f5433/f3904 and FloorLog2PlusOne f68238.
        internal static void Sort<T>(List<T> values, Comparison<T> compare)
        {
            int log = 0;
            for (int n = values.Count; n > 0; n >>= 1) log++;
            IntroSort(values, 0, values.Count - 1, log * 2, compare);
        }
        // f5436/f3909: right recursion, left iteration, insertion cutoff 16.
        private static void IntroSort<T>(List<T> a, int lo, int hi, int depth, Comparison<T> compare)
        {
            while (lo < hi)
            {
                int count = hi - lo + 1;
                if (count <= 16)
                {
                    if (count == 2) SwapIfGreater(a, lo, hi, compare);
                    else if (count == 3)
                    {
                        SwapIfGreater(a, lo, hi - 1, compare);
                        SwapIfGreater(a, lo, hi, compare);
                        SwapIfGreater(a, hi - 1, hi, compare);
                    }
                    else
                    {
                        for (int at = lo; at < hi; at++)
                        {
                            T item = a[at + 1];
                            int j = at;
                            while (j >= lo && compare(item, a[j]) < 0) { a[j + 1] = a[j]; j--; }
                            a[j + 1] = item;
                        }
                    }
                    return;
                }
                if (depth == 0) { HeapSort(a, lo, hi, compare); return; }
                depth--;
                int pivot = Partition(a, lo, hi, compare);
                IntroSort(a, pivot + 1, hi, depth, compare);
                hi = pivot - 1;
            }
        }
        // f3888/f3911: median-of-three and strict scans, even on equal keys.
        private static int Partition<T>(List<T> a, int lo, int hi, Comparison<T> compare)
        {
            int middle = lo + (hi - lo) / 2;
            SwapIfGreater(a, lo, middle, compare);
            SwapIfGreater(a, lo, hi, compare);
            SwapIfGreater(a, middle, hi, compare);
            T pivot = a[middle];
            Swap(a, middle, hi - 1);
            int left = lo, right = hi - 1;
            while (left < right)
            {
                do { left++; } while (compare(a[left], pivot) < 0);
                do { right--; } while (compare(pivot, a[right]) < 0);
                if (left >= right) break;
                Swap(a, left, right);
            }
            Swap(a, left, hi - 1);
            return left;
        }
        // f3887/f3910 and f3889/f3912. Right child wins only on strict less.
        private static void HeapSort<T>(List<T> a, int lo, int hi, Comparison<T> compare)
        {
            int count = hi - lo + 1;
            for (int i = count / 2; i >= 1; i--) DownHeap(a, i, count, lo, compare);
            for (int i = count; i > 1; i--) { Swap(a, lo, lo + i - 1); DownHeap(a, 1, i - 1, lo, compare); }
        }
        private static void DownHeap<T>(List<T> a, int i, int count, int lo, Comparison<T> compare)
        {
            T value = a[lo + i - 1];
            while (i <= count / 2)
            {
                int child = i * 2;
                if (child < count && compare(a[lo + child - 1], a[lo + child]) < 0) child++;
                if (compare(value, a[lo + child - 1]) >= 0) break;
                a[lo + i - 1] = a[lo + child - 1]; i = child;
            }
            a[lo + i - 1] = value;
        }
        private static void SwapIfGreater<T>(List<T> a, int i, int j, Comparison<T> compare)
        { if (i != j && compare(a[i], a[j]) > 0) Swap(a, i, j); }
        private static void Swap<T>(List<T> a, int i, int j)
        { if (i == j) return; T value = a[i]; a[i] = a[j]; a[j] = value; }
    }
}
