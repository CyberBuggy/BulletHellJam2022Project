using System;
using System.Collections;
using System.Collections.Generic;
using BulletFury.Data;
using BulletFury.Rendering;
using BulletFury.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Utils;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

namespace BulletFury
{

#if UNITY_EDITOR
    [InitializeOnLoadAttribute, ExecuteAlways]
    #endif
    public class BulletManager : MonoBehaviour
    {
        #region Serialized Fields
        // <----- Serialized Fields ----->
        // the maximum amount of bullets this bullet manager can show
        [SerializeField, Range(0, 1023)] private int maxBullets = 1023;

        // render priority. Higher number = drawn on top.
        [SerializeField] private int drawPriority = 0;
        
        // the settings for the bullet's behaviour over time
        [SerializeField] private BulletSettings bulletSettings = null;
        public BulletPlane Plane => bulletSettings.Plane;

        // the settings for spawning bullets
        [SerializeField] private SpawnSettings spawnSettings = null;

        [SerializeField] private int currentActiveBullets;
        [SerializeField] private int maxActiveBullets;

        [SerializeField] private string seed;
        [SerializeField] private bool randomiseSeedOnAwake = true;

        [SerializeField] private Material previewMat;
        
        private Squirrel3 _rnd;
        
        public void SetTrackObjectForBullet(Transform toTrack, bool isTracking, int idx)
        {
            bulletSettings.SetTrackedObject(ref _bullets[idx], toTrack);
            _bullets[idx].TrackObject = isTracking ? (byte) 1 : (byte) 0;
        }

        // Unity Event that fires when a bullet reaches end-of-life, can be set in the inspector like a button 
        // ReSharper disable once InconsistentNaming
        [SerializeField] private BulletDiedEvent OnBulletDied;
        public Action<BulletContainer, bool> OnBulletDiedEvent;

        [SerializeField] private BulletDiedEvent OnBulletCancelled;
        public Action<BulletContainer> OnBulletCancelledEvent;
        
        // Unity Event that fires when a bullet is spawned, can be set in the inspector like a button 
        // ReSharper disable once InconsistentNaming
        [SerializeField] private BulletSpawnedEvent OnBulletSpawned;

        [SerializeField] private UnityEvent OnWeaponFired;

        #endregion

        #region Private Fields
        private BulletContainer[] _bullets;
        private bool _hasBullets;
        private BulletContainer _currentBullet;
        private Matrix4x4[] _matrices;
        private MaterialPropertyBlock _materialPropertyBlock;
        private Vector4[] _colors;
        private BulletJob _bulletJob;
        private JobHandle _handle;
        private static readonly int Color = Shader.PropertyToID("_Color");
        private float _currentTime = 0;
        private Vector3 _previousPos;
        private bool _enabled = false;
        private static List<BulletManager> _managers;
        
        #endregion

        /// <summary>
        /// Unity function, happens when the object is first loaded.
        /// Initialise the data.
        /// </summary>
        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (_managers == null)
                _managers = new List<BulletManager>();
            _managers.Add(this);
            
            _bullets = new BulletContainer[maxBullets];
            _matrices = new Matrix4x4[maxBullets];
            _colors = new Vector4[maxBullets];
            _materialPropertyBlock = new MaterialPropertyBlock();
            _previousPos = transform.position;
            if (randomiseSeedOnAwake)
                seed = Guid.NewGuid().ToString();

            _rnd = new Squirrel3(seed.GetHashCode());
            bulletSettings.Setup();
        }
        
        #if UNITY_EDITOR
        static BulletManager()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (_managers == null)
                    return;
                foreach (var manager in _managers)
                {
                    if (manager == null)
                        continue;
                    
                    if (manager.maxActiveBullets < manager.maxBullets - 20)
                        Debug.LogWarning($"Manager {manager.name} has a higher max bullets value than is necessary, consider setting it to {manager.maxActiveBullets} instead.");
                        
                }
            }
        }
        #endif

        public void SetRandomSeed(string newSeed)
        {
            seed = newSeed;
            _rnd = new Squirrel3(seed.GetHashCode());
        }
        
        /// <summary>
        /// Unity function, happens when the object is enabled.
        /// Render the bullets.
        /// </summary>
        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                _enabled = true;
                RenderPipelineManager.beginCameraRendering += Render;
                BulletCollider.AddManagerToColliders(this);
            } else {
                #if UNITY_EDITOR
                SceneView.duringSceneGui += DuringSceneGui;
#endif
            }
        }

        /// <summary>
        /// Unity funciton, happens when the object is disabled.
        /// Stop rendering bullets.
        /// </summary>
        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                _enabled = false;
                RenderPipelineManager.beginCameraRendering -= Render;
                BulletCollider.RemoveManagerFromColliders(this);
                CancelBullets(false);   
            } else {
                #if UNITY_EDITOR
                SceneView.duringSceneGui -= DuringSceneGui;
                #endif
            }
        }

        public void CancelBullets(bool broadcast)
        {
            for (var i = 0; i < _bullets.Length; i++)
            {
                var bullet = _bullets[i];
                if (bullet.Dead == 0)
                {
                    bullet.Dead = 1;
                    if (broadcast)
                    {
                        OnBulletCancelled?.Invoke(i, bullet, true);
                        OnBulletCancelledEvent?.Invoke(bullet);
                    }
                }

                _bullets[i] = bullet;
            }
        }
        
        private bool IsNaN(Quaternion q) {
            return float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w);
        }
        
        /// <summary>
        /// Render the bullets. Called from OnEnable and OnDisable.
        /// Note: this function is called once for every camera.
        /// </summary>
        /// <param name="context">The current scriptable render context</param>
        /// <param name="cam">The camera being used to render</param>
        private void Render(ScriptableRenderContext context, Camera cam)
        {
            if (!gameObject.activeInHierarchy || !_enabled) return;
            
            // create a new buffer - this will contain the render data
            var buffer = new CommandBuffer();
            #if UNITY_EDITOR
            buffer.name = "BulletFury";
            #endif
            
            // create a new material property block - this contains the different colours for every instance
            _materialPropertyBlock = new MaterialPropertyBlock();
            
            _hasBullets = false;
            currentActiveBullets = 0;
            // loop through and render the bullets
            for (int i = _bullets.Length - 1; i >= 0; i--)
            {
                _currentBullet = _bullets[i];
                // if the bullet is alive
                if (_currentBullet.Dead == 0)
                {
                    ++currentActiveBullets;
                    // we've got at least one active bullet, so we should render something
                    _hasBullets = true;

                    // set the colour for the bullet
                    _colors[i] = _currentBullet.Color;
                    
                    // if the "w" part of the rotation is 0, the Quaternion is invalid. Set it to the a rotation of 0,0,0
                    if (_currentBullet.Rotation.w == 0)
                        _currentBullet.Rotation = Quaternion.identity;

                    if (bulletSettings.Plane == BulletPlane.XZ)
                        _currentBullet.Rotation *= Quaternion.AngleAxis(90, Vector3.right);
                    if (IsNaN(_currentBullet.Rotation))
                        _matrices[i] = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
                    else
                        // set the matrix for the current bullet - translation, rotation, scale, in that order.
                        _matrices[i] = Matrix4x4.TRS(_currentBullet.Position,
                            _currentBullet.Rotation,
                            Vector3.one * _currentBullet.CurrentSize);
                }
                else // if the bullet is not alive, position the mesh at 0,0,0, with no rotation, and a scale of 0,0,0, so it isn't displayed.
                    _matrices[i] = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
            }

            // if we don't have any bullets, don't bother rendering anything
            if (!_hasBullets)
                return;

            // set the colours of the material property block
            _materialPropertyBlock.SetVectorArray(Color, _colors);
            
            // draw all the meshes
            // n.b. this is why we can only have 1023 bullets per spawner
            buffer.DrawMeshInstanced(bulletSettings.Mesh, 0, bulletSettings.Material, 0, _matrices, _bullets.Length, _materialPropertyBlock);
            
            // can't have two objects with the same priority, so keep increasing it til we find one that fits
            var priority = drawPriority;
            if (BulletFuryRenderPass.Buffers == null)
                BulletFuryRenderPass.Buffers = new SortedList<int, CommandBuffer>();
            while (BulletFuryRenderPass.Buffers.ContainsKey(priority))
                ++priority;
            
            // add the command buffer to the render pass
            BulletFuryRenderPass.Buffers.Add(priority, buffer);
            maxActiveBullets = Mathf.Max(maxActiveBullets, currentActiveBullets);
        }

        /// <summary>
        /// Unity function, called every frame
        /// Update the values of the bullets that can't be done in a Job, and run the Job
        /// </summary>
        private void Update()
        {
            if (!Application.isPlaying) return;
            var deltaTime = Time.deltaTime;
            // update the bullets according to the settings
            for (int i = _bullets.Length - 1; i >= 0; --i)
                bulletSettings.SetValues(ref _bullets[i], deltaTime, transform.position, _previousPos, gameObject.activeInHierarchy);

            // create a new job
            _bulletJob = new BulletJob
            {
                DeltaTime = deltaTime,
                In = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                Out = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob)
            };

            // start the job
            _handle = _bulletJob.Schedule(_bullets.Length, 256);

            // increment the current timer
            _currentTime += deltaTime;
            _previousPos = transform.position;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (_bullets == null || !gameObject.activeSelf || !_bulletJob.Out.IsCreated )
                return;
            // make sure the job is finished this frame
            _handle.Complete();
            // grab the results
            _bulletJob.Out.CopyTo(_bullets);
            // dispose the native arrays 
            _bulletJob.In.Dispose();
            _bulletJob.Out.Dispose();

            for (int i = _bullets.Length - 1; i >= 0; --i)
            {
                if (_bullets[i].EndOfLife == 1)
                {
                    OnBulletDied?.Invoke(i, _bullets[i], true);
                    OnBulletDiedEvent?.Invoke(_bullets[i], true);
                    _bullets[i].EndOfLife = 0;
                }
            }
        }

        public bool CheckBulletsRemaining()
        {
            if (_bullets == null)
                return false;
            var j = 0;
            // find a bullet that isn't alive and replace it with this one
            for (j = 0; j < _bullets.Length; ++j)
            {
                if (_bullets[j].Dead != 0) break;
            }

            return j < maxBullets - 1;
        }

        public void Spawn(Vector3 position, Vector3 forward)
        {
            var hasBulletsLeft = CheckBulletsRemaining();
            
            // don't spawn a bullet if we haven't reached the correct fire rate
            if (_currentTime < spawnSettings.FireRate || !hasBulletsLeft|| !_enabled)
                return;
            // reset the current time
            _currentTime = 0;
            
            if (gameObject.activeInHierarchy)
                // start the spawning - it's a coroutine so we can do burst shots over time 
                StartCoroutine(SpawnIE(position, forward));
        }
        
        private IEnumerator SpawnIE(Vector3 position, Vector3 forward)
        {
            //yield return new WaitForEndOfFrame();
            OnWeaponFired?.Invoke();
            // keep a list of positions and rotations, so we can update the bullets all at once
            var positions = new List<Vector3>();
            var rotations = new List<Quaternion>();
            for (int burstNum = 0; burstNum < spawnSettings.BurstCount; ++burstNum)
            {
                // make sure the positions and rotations are clear before doing anything
                positions.Clear();
                rotations.Clear();
                // spawn the bullets
                spawnSettings.Spawn((point, dir) =>
                {
                    // for every point that the spawner gets
                    
                    // set up the rotation 
                    if (Plane == BulletPlane.XY)
                    {
                        rotations.Add(Quaternion.LookRotation(Vector3.forward, dir) *
                                      Quaternion.LookRotation(Vector3.forward, forward));
                        
                        positions.Add(position + Quaternion.LookRotation(Vector3.forward, forward) * point);
                    }
                    else
                    {
                        var rotation = dir == Vector2.zero ? Quaternion.identity : Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
                        // rotate it by the direction the object is facing
                        var y =rotation.eulerAngles.y;
                        rotation.SetLookRotation(Quaternion.Euler(0, y, 0) * forward);
                        // add it to the list
                        rotations.Add(rotation);
                        // grab the position, rotated by the direction the object is facing
                        var pos = position + (Quaternion.LookRotation(forward) * new Vector3(point.x, 0, point.y));
                        // at the position to the list
                        positions.Add(pos);
                    }
                }, _rnd);
                
                // for every bullet we found
                for (int i = 0; i < positions.Count; i++)
                {
                    // create a new container that isn't dead, at the position and rotation we found with the spawner
                    var newContainer = new BulletContainer
                    {
                        Dead = 0,
                        Position = positions[i],
                        Rotation = rotations[i],
                        Direction = rotations[i],
                        Id = Guid.NewGuid().GetHashCode()
                    };
                    
                    // initialise the bullet
                    bulletSettings.Init(ref newContainer);
                    
                    var j = 0;
                    // find a bullet that isn't alive and replace it with this one
                    for (j = 0; j < _bullets.Length; ++j)
                    {
                        if (_bullets[j].Dead == 0) continue;
                        _bullets[j] = newContainer;
                        break;
                    }
                    #if UNITY_EDITOR
                    if (j >= _bullets.Length)
                        Debug.LogWarning($"Tried to spawn too many bullets on manager {name}, didn't spawn one.");
                    #endif
                    
                    if (j < _bullets.Length)
                        OnBulletSpawned?.Invoke(j, _bullets[j]);
                }
                
                // wait a little bit before doing the next burst
                yield return new WaitForSeconds(spawnSettings.BurstDelay);
                yield return new WaitForEndOfFrame();
            }
        }
        
        /// <summary>
        /// Activate any waiting bullets.
        /// Use this when you want to do bullet tracing.
        /// </summary>
        public void ActivateWaitingBullets()
        {
            ActivateBullets().Run();
        }

        private IEnumerator ActivateBullets()
        {
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < _bullets.Length; i++)
                _bullets[i].Waiting = 0;
        }
        
        /// <summary>
        /// Get all the bullets in this bullet manager
        /// Used for checking collisions
        /// </summary>
        /// <returns>All the bullets in this bullet manager</returns>
        public BulletContainer[] GetBullets()
        {
            return _bullets;
        }

        /// <summary>
        /// Fire the "Bullet Hit" event and mark the bullet as dead 
        /// </summary>
        public void HitBullet(int idx)
        {
            if (_bullets[idx].Dead == 1) return;
            _bullets[idx].Dead = 1;
            
            OnBulletDied?.Invoke(idx, _bullets[idx], false);
            OnBulletDiedEvent?.Invoke(_bullets[idx], false);
        }

        public BulletContainer GetBullet(int idx)
        {
            return _bullets[idx];
        }
        
        public void WaitBullet(int idx)
        {
            var b = _bullets[idx];
            b.Waiting = 1;
            _bullets[idx] = b;
        }

        public void SetSpawnSettings(SpawnSettings settings)
        {
            spawnSettings = settings;
        }

        public void SetBulletSettings(BulletSettings settings)
        {
            bulletSettings = settings;
        }

        public SpawnSettings GetSpawnSettings()
        {
            return spawnSettings;
        }

        public BulletSettings GetBulletSettings()
        {
        return bulletSettings;
        }

#if UNITY_EDITOR
        private static Squirrel3 _editorRnd;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private BulletContainer[] _editorBullets;
        private SceneView _sceneView;
        public void AnimateAlongPath()
        {
            _editorBullets = new BulletContainer[maxBullets];
            for (int i = 0; i < _editorBullets.Length; i++)
                _editorBullets[i].Dead = 1;
            
            EditorCoroutineUtility.StartCoroutineOwnerless(SpawnPreview(transform.position, transform.forward));
            EditorCoroutineUtility.StartCoroutineOwnerless(UpdateEditorBullets());
        }
        
        private IEnumerator SpawnPreview(Vector3 position, Vector3 forward)
        {
            // keep a list of positions and rotations, so we can update the bullets all at once
            var positions = new List<Vector3>();
            var rotations = new List<Quaternion>();
            for (int burstNum = 0; burstNum < spawnSettings.BurstCount; ++burstNum)
            {
                // make sure the positions and rotations are clear before doing anything
                positions.Clear();
                rotations.Clear();
                
                // spawn the bullets
                spawnSettings.Spawn((point, dir) =>
                {
                    // for every point that the spawner gets
                    
                    // set up the rotation 
                    if (Plane == BulletPlane.XY)
                    {
                        rotations.Add(Quaternion.LookRotation(Vector3.forward, dir) *
                                      Quaternion.LookRotation(Vector3.forward, forward));
                        
                        positions.Add(position + Quaternion.LookRotation(Vector3.forward, forward) * point);
                    }
                    else
                    {
                        var rotation = dir == Vector2.zero ? Quaternion.identity : Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));

                        // rotate it by the direction the object is facing
                        var y =rotation.eulerAngles.y;
                        rotation.SetLookRotation(Quaternion.Euler(0, y, 0) * forward);
                        // add it to the list
                        rotations.Add(rotation);
                        // grab the position, rotated by the direction the object is facing
                        var pos = position + (Quaternion.LookRotation(forward) * new Vector3(point.x, 0, point.y));
                        // at the position to the list
                        positions.Add(pos);
                    }
                }, _rnd);
                
                // for every bullet we found
                for (int i = 0; i < positions.Count; i++)
                {
                    // create a new container that isn't dead, at the position and rotation we found with the spawner
                    var newContainer = new BulletContainer
                    {
                        Dead = 0,
                        Position = positions[i],
                        Rotation = rotations[i],
                        Id = Guid.NewGuid().GetHashCode()
                    };
                    
                    // initialise the bullet
                    bulletSettings.Init(ref newContainer);
                    
                    var j = 0;
                    // find a bullet that isn't alive and replace it with this one
                    for (j = 0; j < _editorBullets.Length; ++j)
                    {
                        if (_editorBullets[j].Dead == 0) continue;
                        _editorBullets[j] = newContainer;
                        break;
                    }
#if UNITY_EDITOR
                    if (j >= _editorBullets.Length)
                        Debug.LogWarning($"Tried to spawn too many bullets on manager {name}, didn't spawn one.");
#endif
                }
                
                // wait a little bit before doing the next burst
                yield return new WaitForSeconds(spawnSettings.BurstDelay);
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator UpdateEditorBullets()
        {
            var deltaTime = 1/60f;
            var timer = bulletSettings.Lifetime;
            while (timer > 0)
            {
                // update the bullets according to the settings
                for (int i = _editorBullets.Length - 1; i >= 0; --i)
                {
                    bulletSettings.SetValues(ref _editorBullets[i], deltaTime, transform.position, _previousPos, gameObject.activeInHierarchy);
                    
                    _editorBullets[i].CurrentLifeSeconds += deltaTime;
                    if (_editorBullets[i].CurrentLifeSeconds > _editorBullets[i].Lifetime)
                    {
                        _editorBullets[i].Dead = 1;
                        _editorBullets[i].EndOfLife = 1;
                    }

                    _editorBullets[i].CurrentLifePercent = _editorBullets[i].CurrentLifeSeconds / _editorBullets[i].Lifetime;
                    _editorBullets[i].Position += _editorBullets[i].Velocity * deltaTime +
                                          _editorBullets[i].Force * deltaTime;

           
                    _editorBullets[i].Rotation =  Quaternion.Normalize(_editorBullets[i].Rotation);
                }
                
                _previousPos = transform.position;
                _sceneView.Repaint();
                
                yield return new WaitForSeconds(deltaTime);
                timer -= deltaTime;
            }

            _editorBullets = null;
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            if (Selection.activeGameObject != gameObject || spawnSettings == null || bulletSettings == null || previewMat == null) return;
            if (_sceneView == null)
                _sceneView = sceneView;
            if (_editorRnd == null)
                _editorRnd = new Squirrel3();

            var forward = transform.forward;
            var position = transform.position;

            if (_editorBullets != null)
            {
                foreach (var b in _editorBullets)
                {
                    if (b.Dead == 1) continue;
                    var finalPos = b.Position;
                    var rot = b.Rotation * Quaternion.Euler(90, 0, 0);
                    var size = b.CurrentSize;
                    if (Mathf.Approximately(rot.w, 0) && Mathf.Approximately(rot.x, 0) && Mathf.Approximately(rot.y, 0) && Mathf.Approximately(rot.z, 0))
                        rot = Quaternion.identity;
                    var mtx = Matrix4x4.TRS(finalPos, rot,
                        Vector3.one * size);

                    
                    previewMat.SetColor(BaseColor, b.Color);
                    
                    Graphics.DrawMesh(bulletSettings.Mesh, mtx, previewMat, gameObject.layer, sceneView.camera);
                    Handles.color = UnityEngine.Color.green;
                    Handles.DrawWireArc(finalPos, Vector3.forward, Vector3.up, 360,
                        bulletSettings.ColliderSize * bulletSettings.Size);
                    Handles.color = UnityEngine.Color.white;
                }

                return;
            }

            
            spawnSettings.Spawn((point, dir) =>
            {
                // for every point that the spawner gets

                var color = bulletSettings.StartColor;
                previewMat.SetColor(BaseColor, color);
                previewMat.SetTexture(BaseMap, bulletSettings.Material.mainTexture);
                // set up the rotation 
                if (Plane == BulletPlane.XY)
                {
                    var rot = Quaternion.LookRotation(Vector3.forward, dir) *
                                  Quaternion.LookRotation(Vector3.forward, forward);

                    var finalPos = position + Quaternion.LookRotation(Vector3.forward, forward) * point;
                    var mtx = Matrix4x4.TRS(finalPos, rot,
                        Vector3.one * bulletSettings.Size);
                    
                    Graphics.DrawMesh(bulletSettings.Mesh, mtx, previewMat, gameObject.layer, sceneView.camera);
                    Handles.color = UnityEngine.Color.green;
                    Handles.DrawWireArc(finalPos, Vector3.forward, Vector3.up, 360, bulletSettings.ColliderSize * bulletSettings.Size);
                    Handles.color = UnityEngine.Color.white;
                }
                else
                {
                    var rotation = dir == Vector2.zero ? Quaternion.identity : Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));

                    // rotate it by the direction the object is facing
                    var y =rotation.eulerAngles.y;
                    rotation.SetLookRotation(Quaternion.Euler(0, y, 0) * forward);
                    // add it to the list
                    //rotations.Add(rotation);
                    // grab the position, rotated by the direction the object is facing
                    var finalPos = position + (Quaternion.LookRotation(forward) * new Vector3(point.x, 0, point.y));
                    
                    rotation *= Quaternion.Euler(90, 0, 0);
                    
                    var mtx = Matrix4x4.TRS(finalPos, rotation,
                        Vector3.one * bulletSettings.Size);
                    Graphics.DrawMesh(bulletSettings.Mesh, mtx, previewMat, gameObject.layer, sceneView.camera);
                    //bulletSettings.Material.SetPass(0);
                    //Graphics.DrawMeshNow(bulletSettings.Mesh, mtx);
                    Handles.color = UnityEngine.Color.green;
                    Handles.DrawWireArc(finalPos, Vector3.up, Vector3.forward, 360, bulletSettings.ColliderSize * bulletSettings.Size);
                    Handles.color = UnityEngine.Color.white;
                    // at the position to the list
                    //positions.Add(pos);
                }
            }, _editorRnd);
        }
        #endif
        public static List<BulletManager> GetAllManagers()
        {
            return _managers;
        }
    }
}