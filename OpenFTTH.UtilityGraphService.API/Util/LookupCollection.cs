using OpenFTTH.UtilityGraphService.API.Model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenFTTH.UtilityGraphService.API.Util
{
    /// <summary>
    /// Collection that will be serialized as an array of identified objects, but internally use a concurrent dictionary 
    /// for fast lookup by uuid. If you keep the objects in collection immutable, then access to the collection and its
    /// objects will be thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LookupCollection<T> : IEnumerable<T> where T : IIdentifiedObject
    {
        private readonly ConcurrentDictionary<Guid, T> _objectsDict = new ConcurrentDictionary<Guid, T>();

        public LookupCollection()
        {
        }

        public LookupCollection(IEnumerable<T> objects)
        {
            AddRange(objects);
        }

        public bool TryGetValue(Guid key, out T? value)
        {
            if (_objectsDict.TryGetValue(key, out var obj))
            {
                value = obj;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public T this[Guid key]
        {
            get => _objectsDict[key];

            set => _objectsDict[key] = value;
        }

        public bool ContainsKey(Guid key)
        {
            return _objectsDict.ContainsKey(key);
        }

        public int Count => _objectsDict.Count;

        public void Add(T obj)
        {
            _objectsDict.TryAdd(obj.Id, obj);
        }

        public void AddRange(IEnumerable<T> objects)
        {
            foreach (var obj in objects)
            {
                _objectsDict.TryAdd(obj.Id, obj);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
           return ((IEnumerator<T>)_objectsDict.Values.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _objectsDict.Values.GetEnumerator();
        }
    }
}
