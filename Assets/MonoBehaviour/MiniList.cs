using System;
using System.Collections;
using System.Collections.Generic;

class MiniList<T> : IEnumerable<T> {
    public int Count { get; private set; }
    private int Pos;
    private readonly T[] Array;

    public MiniList(int maxSize) {
        Array = new T[maxSize];
    }

    public bool Empty {
        get { return Count == 0; }
    }

    public void Add(T ele) {
        if (Count == Array.Length) {
            throw new IndexOutOfRangeException("Impossible adding more element, array size (" + Count + ")");
        }

        Array[Count++] = ele;
    }

    public bool Contains(T ele) {
        for (int x = 0; x < Count; x++) {
            if (Array[x].Equals(ele)) {
                return true;
            }
        }

        return false;
    }

    public T this[int index] {
        get {
            if (index > Count) {
                throw new IndexOutOfRangeException("Index: " + index + ", Count: " + Count);
            }

            return Array[index];
        }

        set {
            if (index > Count) {
                throw new IndexOutOfRangeException("Index: " + index + ", Count: " + Count);
            }

            Array[index] = value;
        }
    }

    public IEnumerator<T> GetEnumerator() {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public struct Enumerator : IEnumerator<T> {
        private int x;
        private MiniList<T> miniList;

        internal Enumerator(MiniList<T> miniList) {
            this.miniList = miniList;
            x = -1;
        }

        public bool MoveNext() {
            x++;
            return x < miniList.Count;
        }

        public void Reset() {
            x = 0;
        }

        public T Current {
            get { return miniList[x]; }
            private set { }
        }

        object IEnumerator.Current {
            get { return Current; }
        }

        public void Dispose() {
            miniList = null;
        }
    }
}