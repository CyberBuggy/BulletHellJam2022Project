using System;
using BulletFury.Data;
using UnityEditor;
using UnityEngine;

namespace BulletFury.Editor
{
    [CustomPropertyDrawer(typeof(BulletSettings))]
    public class BulletSettingsPropertyDrawer : PropertyDrawer
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

                if (GUI.Button(currentPos, "Create New Bullet Settings"))
                {
                    var obj = ScriptableObject.CreateInstance<BulletSettings>();
                    AssetDatabase.CreateAsset(obj, "Assets/New Bullet Settings.asset");
                    property.objectReferenceValue = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                return;
            }

            //currentPos.y += height;
            //++numFields;
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
            EditorGUI.indentLevel++;
                

            EditorGUI.indentLevel--;
            BulletFuryEditorUtils.AddLabel("Visual Settings", height, ref currentPos, ref numFields);
            EditorGUI.indentLevel++;

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "mesh", height, ref currentPos, ref numFields);

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "material", height, ref currentPos, ref numFields);
            
            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "plane", height, ref currentPos, ref numFields);
            
            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "damage", height, ref currentPos, ref numFields);

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "startColor", height, ref currentPos, ref numFields);

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "size", height, ref currentPos, ref numFields);
            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "colliderSize", height, ref currentPos, ref numFields);

            EditorGUI.indentLevel--;
            BulletFuryEditorUtils.AddLabel("Behaviour", height, ref currentPos, ref numFields);
            EditorGUI.indentLevel++;

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "waitToStart", height, ref currentPos, ref numFields);
                
            if (serializedObject.FindProperty("waitToStart").boolValue)
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "timeToPlayWhileWaiting", height, ref currentPos, ref numFields);

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "lifetime", height, ref currentPos, ref numFields);

            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "speed", height, ref currentPos, ref numFields);
            
            BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "moveWithTransform", height, ref currentPos, ref numFields);

            EditorGUI.indentLevel--;
            BulletFuryEditorUtils.AddLabel("Time-based Properties", height, ref currentPos, ref numFields);
            EditorGUI.indentLevel++;
                
            currentPos.y += height;
            ++numFields;
            currentPos.x += height;
            _expandColor = EditorGUI.Foldout(currentPos, _expandColor, "Color Over Time");
            currentPos.x -= height;
                
            var checkboxPos = currentPos;
            checkboxPos.x = position.x - height;
            checkboxPos.width = height * 2;
                
            EditorGUI.PropertyField(checkboxPos, serializedObject.FindProperty("useColorOverTime"), GUIContent.none,
                true);

            var enabled = serializedObject.FindProperty("useColorOverTime").boolValue;
            if (_expandColor)
            {
                EditorGUI.BeginDisabledGroup(!enabled);
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "colorCurveUsage", height, ref currentPos, ref numFields);
                if ((CurveUsage)serializedObject.FindProperty("colorCurveUsage").enumValueIndex == CurveUsage.LoopedTime)
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "colorCurveTime", height, ref currentPos, ref numFields);
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "colorOverTime", height, ref currentPos, ref numFields);
                EditorGUI.EndDisabledGroup();
            }

            currentPos.y += height;
            ++numFields;
            currentPos.x += height;
            _expandRotation = EditorGUI.Foldout(currentPos, _expandRotation, "Rotation Over Time");
            currentPos.x -= height;
            checkboxPos.y = currentPos.y;
            EditorGUI.PropertyField(checkboxPos, serializedObject.FindProperty("useRotationOverTime"), GUIContent.none,
                true);
            enabled = serializedObject.FindProperty("useRotationOverTime").boolValue;
            if (_expandRotation)
            {
                EditorGUI.BeginDisabledGroup(!enabled);
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "rotationCurveUsage", height, ref currentPos, ref numFields);
                if ((CurveUsage)serializedObject.FindProperty("rotationCurveUsage").enumValueIndex == CurveUsage.LoopedTime)
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "rotationCurveTime", height, ref currentPos, ref numFields);
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "trackObject", height, ref currentPos, ref numFields);
                if (serializedObject.FindProperty("trackObject").boolValue)
                {
                    currentPos.y += height * 1.25f;
                    ++numFields;
                    serializedObject.FindProperty("trackedObjectTag").stringValue = EditorGUI.TagField(currentPos, serializedObject.FindProperty("trackedObjectTag").stringValue);
                    
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "turnSpeed" ,height, ref currentPos, ref numFields);
                }
                else
                {
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "angularVelocity", height, ref currentPos, ref numFields);
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "rotationOverTime", height, ref currentPos, ref numFields);
                }
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "useRotationForDirection", height, ref currentPos, ref numFields);
                
                EditorGUI.EndDisabledGroup();
            }

            currentPos.y += height;
            ++numFields;
            currentPos.x += height;
            _expandSize = EditorGUI.Foldout(currentPos, _expandSize, "Size Over Time");
            currentPos.x -= height;
            checkboxPos.y = currentPos.y;
            EditorGUI.PropertyField(checkboxPos, serializedObject.FindProperty("useSizeOverTime"), GUIContent.none,
                true);
            enabled = serializedObject.FindProperty("useSizeOverTime").boolValue;
            if (_expandSize)
            {
                EditorGUI.BeginDisabledGroup(!enabled);
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "sizeCurveUsage", height, ref currentPos, ref numFields);
                if ((CurveUsage)serializedObject.FindProperty("sizeCurveUsage").enumValueIndex == CurveUsage.LoopedTime)
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "sizeCurveTime", height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "sizeOverTime", height, ref currentPos, ref numFields);
                EditorGUI.EndDisabledGroup();
            }

            currentPos.y += height;
            ++numFields;
            currentPos.x += height;
            _expandVelocity = EditorGUI.Foldout(currentPos, _expandVelocity, "Velocity Over Time");
            currentPos.x -= height;
            checkboxPos.y = currentPos.y;
            EditorGUI.PropertyField(checkboxPos, serializedObject.FindProperty("useVelocityOverTime"), GUIContent.none,
                true);
            enabled = serializedObject.FindProperty("useVelocityOverTime").boolValue;
            if (_expandVelocity)
            {
                EditorGUI.BeginDisabledGroup(!enabled);
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "velocityCurveUsage", height, ref currentPos, ref numFields);
                if ((CurveUsage)serializedObject.FindProperty("velocityCurveUsage").enumValueIndex == CurveUsage.LoopedTime)
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "velocityCurveTime", height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "velocitySpace", height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "scaleWithSpeed", height, ref currentPos, ref numFields);

                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "velocityScaleInDirection", height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeCurve(ref serializedObject, "velocityOverTimeX", Color.red, height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeCurve(ref serializedObject, "velocityOverTimeY", Color.green, height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeCurve(ref serializedObject, "velocityOverTimeZ", Color.yellow, height, ref currentPos, ref numFields);

                EditorGUI.EndDisabledGroup();
            }

            currentPos.y += height;
            ++numFields;
            currentPos.x += height;
            _expandForce = EditorGUI.Foldout(currentPos, _expandForce, "Force Over Time");
            currentPos.x -= height;
            checkboxPos.y = currentPos.y;
            EditorGUI.PropertyField(checkboxPos, serializedObject.FindProperty("useForceOverTime"), GUIContent.none,
                true);
            enabled = serializedObject.FindProperty("useForceOverTime").boolValue;
            if (_expandForce)
            {
                EditorGUI.BeginDisabledGroup(!enabled);
                
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "forceCurveUsage", height, ref currentPos, ref numFields);
                if ((CurveUsage)serializedObject.FindProperty("forceCurveUsage").enumValueIndex == CurveUsage.LoopedTime)
                    BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "forceCurveTime", height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "forceSpace", height, ref currentPos, ref numFields);

                BulletFuryEditorUtils.AddRelativeCurve(ref serializedObject, "forceOverTimeX", Color.red, height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeProperty(ref serializedObject, "forceScaleInDirection", height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeCurve(ref serializedObject, "forceOverTimeY", Color.green, height, ref currentPos, ref numFields);
                BulletFuryEditorUtils.AddRelativeCurve(ref serializedObject, "forceOverTimeZ", Color.yellow, height, ref currentPos, ref numFields);

                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
            property.objectReferenceValue = serializedObject.targetObject;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return height * numFields * 1.25f;
        }

    }
}