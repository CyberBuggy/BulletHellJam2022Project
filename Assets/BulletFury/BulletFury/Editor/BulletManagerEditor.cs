using UnityEditor;
using UnityEngine;

namespace BulletFury.Editor
{
    [CustomEditor(typeof(BulletManager))]
    public class BulletManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _maxBullets;
        private SerializedProperty _drawPriority;
        private SerializedProperty _bulletSettings;
        private SerializedProperty _spawnSettings;
        private SerializedProperty _currentActiveBullets;
        private SerializedProperty _maxActiveBullets;
        private SerializedProperty _onBulletDied;
        private SerializedProperty _onBulletSpawned;
        private SerializedProperty _onBulletCancelled;
        private SerializedProperty _onWeaponFired;
        
        private SerializedProperty _seed;
        private SerializedProperty _randomiseSeedOnAwake;

        private void OnEnable()
        {
            _maxBullets = serializedObject.FindProperty("maxBullets");
            _drawPriority = serializedObject.FindProperty("drawPriority");
            _bulletSettings = serializedObject.FindProperty("bulletSettings");
            _spawnSettings = serializedObject.FindProperty("spawnSettings");
            _currentActiveBullets = serializedObject.FindProperty("currentActiveBullets");
            _maxActiveBullets = serializedObject.FindProperty("maxActiveBullets");
            _onBulletDied = serializedObject.FindProperty("OnBulletDied");
            _onBulletSpawned = serializedObject.FindProperty("OnBulletSpawned");
            _onBulletCancelled = serializedObject.FindProperty("OnBulletCancelled");
            _onWeaponFired = serializedObject.FindProperty("OnWeaponFired");
            _seed = serializedObject.FindProperty("seed");
            _randomiseSeedOnAwake = serializedObject.FindProperty("randomiseSeedOnAwake");
        }

        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            BulletFuryEditorUtils.DrawProperty(_maxBullets);
            if (_maxBullets.intValue == 1023)
                EditorGUILayout.HelpBox("Hint: Set this to the max (1023) and run the game. The values below will show you how many bullets the spawner is actually using - for the best results, keep close to the \"Max Active Bullets\" value. If it's always 1023, it is probably trying to spawn too many bullets - consider using two bullet managers instead.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            BulletFuryEditorUtils.DrawProperty(_currentActiveBullets);
            BulletFuryEditorUtils.DrawProperty(_maxActiveBullets);
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            if (_maxActiveBullets.intValue > 0 && _maxBullets.intValue > _maxActiveBullets.intValue + 20)
                EditorGUILayout.HelpBox("Max bullets is much greater than the maximum number of bullets used, for the best performance use a similar value.", MessageType.Warning);
            else if (_maxActiveBullets.intValue > 0 && _maxBullets.intValue < _maxActiveBullets.intValue)
                EditorGUILayout.HelpBox("Max bullets is fewer than the number of bullets used, this will cause issues", MessageType.Warning);
            EditorGUILayout.Space();

            BulletFuryEditorUtils.DrawProperty(_drawPriority);
            
            BulletFuryEditorUtils.DrawProperty(_bulletSettings);
            BulletFuryEditorUtils.DrawProperty(_spawnSettings);
            EditorGUILayout.Space();
            BulletFuryEditorUtils.DrawProperty(_onBulletDied);            
            BulletFuryEditorUtils.DrawProperty(_onBulletSpawned);
            BulletFuryEditorUtils.DrawProperty(_onBulletCancelled);
            BulletFuryEditorUtils.DrawProperty(_onWeaponFired);
            BulletFuryEditorUtils.DrawProperty(_seed);
            BulletFuryEditorUtils.DrawProperty(_randomiseSeedOnAwake);
            
            if (GUILayout.Button("Preview"))
                ((BulletManager) target).AnimateAlongPath();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}