using UnityEngine;

namespace Ktyl.Util
{
    public abstract partial class SerialVar<T> : ScriptableObject
    {
        public T Value
        {
            get => Application.isPlaying ? _value : _initialValue;
            set
            {
                if (_readOnly)
                {
                    Debug.LogError("tried to write to read only variable", this);
                    return;
                }
                
                _value = value;   
            }
        }
        private T _value;

        [SerializeField] private T _initialValue;
        [SerializeField] private bool _readOnly;
        
        public static implicit operator T(SerialVar<T> t) => t.Value;

        private void OnValidate()
        {
            _value = _initialValue;
        }

        private void OnEnable()
        {
            _value = _initialValue;
        }

        public override string ToString() => Value.ToString();
        
        #if UNITY_EDITOR
        public T EDITOR_InitialValue
        {
            get => _initialValue;
            set => _initialValue = value;
        }
        #endif
    }
}