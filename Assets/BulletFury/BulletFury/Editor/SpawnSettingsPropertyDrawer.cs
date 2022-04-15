using System;
using BulletFury.Data;
using UnityEditor;
using UnityEngine;

namespace BulletFury.Editor
{
    [CustomPropertyDrawer(typeof(SpawnSettings))]
    public class SpawnSettingsPropertyDrawer : PropertyDrawer
    {
        private const float height = 18f;
        private int numFields = 5;

        private bool _expandColor;
        private bool _expandRotation;
        private bool _expandSize;
        private bool _expandVelocity;
        private bool _expandForce;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Math.Abs(position.height - 1) < Mathf.Epsilon)
                return;
            var currentPos = position;
            currentPos.height = height;

            numFields = 0;
            ++numFields;
            EditorGUI.LabelField(currentPos, label, EditorStyles.boldLabel); 
            EditorGUI.PropertyField(currentPos, property);

            if (property.objectReferenceValue == null)
            {
                currentPos.y += height * 1.25f;
                ++numFields;

                if (GUI.Button(currentPos, "Create New Spawn Settings"))
                {
                    var obj = ScriptableObject.CreateInstance<SpawnSettings>();
                    AssetDatabase.CreateAsset(obj, "Assets/New Spawn Settings.asset");

                    property.objectReferenceValue = obj;
                    EditorGUIUtility.PingObject(obj);
                }

                return;
            }
            
            var serializedObject = new SerializedObject(property.objectReferenceValue);
            serializedObject.Update();

            serializedObject.FindProperty("isExpanded").boolValue = EditorGUI.Foldout(currentPos, serializedObject.FindProperty("isExpanded").boolValue, "");

            if (!serializedObject.FindProperty("isExpanded").boolValue)
            {
                serializedObject.ApplyModifiedProperties();
                property.objectReferenceValue = serializedObject.targetObject;
                return;
            }
            
            EditorGUI.indentLevel++;
                

            AddRelativeProperty(ref serializedObject, "fireRate", ref currentPos);

            AddRelativeProperty(ref serializedObject, "burstCount", ref currentPos);

            if (serializedObject.FindProperty("burstCount").boolValue)
                AddRelativeProperty(ref serializedObject, "burstDelay", ref currentPos);

            AddRelativeProperty(ref serializedObject, "spawnDir", ref currentPos);
            var spawnDir = (SpawnDir) serializedObject.FindProperty("spawnDir").enumValueIndex;
            
            if (spawnDir == SpawnDir.Randomised)
                AddRelativeProperty(ref serializedObject, "directionArc", ref currentPos);

            AddRelativeProperty(ref serializedObject, "numSides", ref currentPos);
            if (serializedObject.FindProperty("numSides").intValue < 1)
                serializedObject.FindProperty("numSides").intValue = 1;
            
            AddRelativeProperty(ref serializedObject, "numPerSide", ref currentPos);
            
            AddRelativeProperty(ref serializedObject, "radius", ref currentPos);
            
            AddRelativeProperty(ref serializedObject, "randomise", ref currentPos);
            
            if (spawnDir == SpawnDir.Randomised || serializedObject.FindProperty("randomise").boolValue)
                AddRelativeProperty(ref serializedObject, "onEdge", ref currentPos);
            
            if (spawnDir == SpawnDir.Directional && Mathf.Approximately(serializedObject.FindProperty("radius").floatValue, 0))
            {
                currentPos.y += height;
                currentPos.y += height / 2;
                currentPos.height = height * 2;
                ++numFields;
                ++numFields;
                ++numFields;
                EditorGUI.HelpBox(currentPos, "If the radius is zero, a Directional spawn direction will appear to do nothing", MessageType.Warning);
                currentPos.height = height;
                currentPos.y += height;
                currentPos.y += height / 2;
            }
            
            AddRelativeProperty(ref serializedObject, "arc", ref currentPos);

            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
            property.objectReferenceValue = serializedObject.targetObject;
        }

        private void AddRelativeProperty(ref SerializedObject property, string name, ref Rect currentPos)
        {
            currentPos.y += height * 1.25f;
            ++numFields;
            EditorGUI.PropertyField(currentPos, property.FindProperty(name), true);
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return height * numFields * 1.25f;
        }
    }
}