using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Markdown.MAML.Model.MAML
{
    public sealed class MamlPropertySet<T> : IEnumerable<T> where T : INamed
    {
        private Dictionary<string, T> _Item;

        private List<T> _Index;

        public MamlPropertySet()
        {
            _Item = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            _Index = new List<T>();
        }

        public T this[string property]
        {
            get
            {
                if (!_Item.ContainsKey(property))
                {
                    return default(T);
                }

                return _Item[property];
            }
        }

        public T this[int index]
        {
            get
            {
                if (_Index.Count <= index)
                {
                    return default(T);
                }

                return _Index[index];
            }
        }

        public int Count
        {
            get { return _Index.Count; }
        }

        public void Add(T property)
        {
            _Item[property.Name] = property;
            _Index.Add(property);
        }

        public bool ContainsKey(string name)
        {
            return _Item.ContainsKey(name);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _Index.GetEnumerator();
        }

        public void Remove(string name)
        {
            if (_Item.ContainsKey(name))
            {
                _Index.Remove(_Item[name]);
                _Item.Remove(name);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Index.GetEnumerator();
        }
    }
}
