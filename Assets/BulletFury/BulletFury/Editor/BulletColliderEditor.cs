using UnityEditor;

namespace BulletFury.Editor
{
    [CustomEditor(typeof(BulletCollider)), CanEditMultipleObjects]
    public class BulletColliderEditor : UnityEditor.Editor
    {
        private SerializedProperty _hitByBulletsList;
        private SerializedProperty _radius;
        private SerializedProperty _onCollide;
        private SerializedProperty _shape;
        private SerializedProperty _bounds;
        private SerializedProperty _offset;
        private SerializedProperty _pointA;
        private SerializedProperty _pointB;
        private SerializedProperty _pointC;
        private SerializedProperty _destroyBullet;
        private SerializedProperty _bulletLayersToBeHitBy;
        
        private void OnEnable()
        {
            _hitByBulletsList = serializedObject.FindProperty("hitByBullets");
            _radius = serializedObject.FindProperty("radius");
            _onCollide = serializedObject.FindProperty("OnCollide");
            _shape = serializedObject.FindProperty("shape");
            _bounds = serializedObject.FindProperty("size");
            _offset = serializedObject.FindProperty("center");
            _pointA = serializedObject.FindProperty("pointA");
            _pointB = serializedObject.FindProperty("pointB");
            _pointC = serializedObject.FindProperty("pointC");
            _bulletLayersToBeHitBy = serializedObject.FindProperty("bulletLayersToBeHitBy");
            _destroyBullet = serializedObject.FindProperty("destroyBullet");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_bulletLayersToBeHitBy);

            EditorGUILayout.PropertyField(_shape);

            if ((ColliderShape) _shape.enumValueIndex != ColliderShape.Triangle)
            {
                EditorGUILayout.PropertyField(_offset);
                EditorGUILayout.PropertyField((ColliderShape) _shape.enumValueIndex == ColliderShape.Sphere
                    ? _radius
                    : _bounds);
            }
            else
            {
                EditorGUILayout.PropertyField(_pointA);
                EditorGUILayout.PropertyField(_pointB);
                EditorGUILayout.PropertyField(_pointC);
            }

            EditorGUILayout.PropertyField(_onCollide);
            if ((ColliderShape)_shape.enumValueIndex == ColliderShape.OBB)
                EditorGUILayout.HelpBox("Hint: Do you need rotated boxes? This has significant performance implications, use axis-aligned bounding boxes where possible", MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}