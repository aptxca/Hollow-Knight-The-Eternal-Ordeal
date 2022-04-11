using System;
using UnityEngine;

namespace Demo2D.Frame
{
    [Serializable]
    public class ReactiveProperty<T> where T : IEquatable<T>
    {
        [SerializeField]
        private T _data = default;
        public T Data
        {
            get => _data;
            set
            {
                if (!_data.Equals(value)||value.Equals(default))
                {
                    _data = value;
                    onValueChanged?.Invoke(_data);
                }
            }
        }
        public ReactiveProperty(T data)
        {
            this._data = data;
        }
        public Action<T> onValueChanged;
    }

}