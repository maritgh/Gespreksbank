using System;
using Unity.AI.Toolkit.Utility;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Animate.Services.SessionPersistence
{
    [Serializable]
    class ObjectPersistence : ScriptableSingleton<ObjectPersistence>
    {
        [SerializeReference]
        SerializableDictionary<string, object> data = new();

        void OnEnable() => data ??= new();

        // A stored entry can come back null after a domain reload; fall back to a fresh instance.
        public T Get<T>(string key) where T: class, new() => data.TryGetValue(key, out var value) && value is T typed ? typed : new T();
        public void Set(string key, object obj) => data[key] = obj;
    }
}
