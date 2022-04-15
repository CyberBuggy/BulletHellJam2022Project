#if UNITY_EDITOR
using UnityEditor;
namespace Ktyl.Util
{
    public abstract class SerialVarEditor<T> : Editor
    {
        private SerialVar<T> _t;

        protected virtual void OnEnable()
        {
            _t = target as SerialVar<T>;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Current Value", _t.Value.ToString());
            EditorGUI.EndDisabledGroup();
        }
    }
    
}
#endif
