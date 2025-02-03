using System;
using System.Collections.Generic;
using System.Linq;
[Serializable]
public class MyDictionary<T,T1> {
    public List<DictItem<T, T1>> items;

    public MyDictionary(Dictionary<T, T1> dictionary) {
        items = new();
        foreach (var dict in dictionary) {
            items.Add(new DictItem<T, T1>() {
                key = dict.Key,
                value = dict.Value
            });
        }
    }

    public Dictionary<T, T1> GetDictionary() => items.ToDictionary(item => item.key, item => item.value);
    public bool ContainEqualsKey() {
        return items.Any(item => items.Count(x => x.Equals(item)) > 1);
    }

    [Serializable]
    public class DictItem<T,T1> : IEquatable<DictItem<T,T1>> {
        public T key;
        public T1 value;
        public bool Equals(DictItem<T, T1> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.key.Equals(key);
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DictItem<T, T1>)obj);
        }
        public override int GetHashCode() {
            return HashCode.Combine(key, value);
        }
    }
}
