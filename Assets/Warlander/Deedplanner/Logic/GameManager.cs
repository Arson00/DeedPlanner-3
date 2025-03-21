﻿using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Inputs;
using Warlander.Deedplanner.Updaters;
using Zenject;

namespace Warlander.Deedplanner.Logic
{
    public class GameManager : MonoBehaviour
    {
        [Inject] private IInstantiator _instantiator;
        [Inject] private DPInput _input;
        [Inject] private AbstractUpdater[] _updaters;

        [SerializeField] private OverlayMesh overlayMeshPrefab = null;
        [SerializeField] private Mesh heightmapHandleMesh = null;
        
        public event Action MapInitialized;
        public event Action RenderSettingsChanged;
        
        public Map Map { get; private set; }

        public OverlayMesh OverlayMeshPrefab => overlayMeshPrefab;
        public Mesh HeightmapHandleMesh => heightmapHandleMesh;

        public bool RenderDecorations { get; private set; } = true;
        public bool RenderTrees { get; private set; } = true;
        public bool RenderBushes { get; private set; } = true;
        public bool RenderShips { get; private set; } = true;
        
        private void Start()
        {
            LayoutManager.Instance.TabChanged += OnTabChange;
            
            _input.EditingControls.Undo.performed += UndoOnperformed;
            _input.EditingControls.Redo.performed += RedoOnperformed;
        }
        
        private void UndoOnperformed(InputAction.CallbackContext obj)
        {
            Map.CommandManager.Undo();
        }
        
        private void RedoOnperformed(InputAction.CallbackContext obj)
        {
            Map.CommandManager.Redo();
        }

        public void CreateNewMap(int width, int height)
        {
            if (Map)
            {
                Destroy(Map.gameObject);
            }
            
            Map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            Map.Initialize(width, height);
            MapInitialized?.Invoke();
        }

        public void ResizeMap(int left, int right, int bottom, int top)
        {
            Map.gameObject.SetActive(false);
            Map newMap = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            newMap.Initialize(Map, left, right, bottom, top);
            Destroy(Map.gameObject);
            Map = newMap;
            MapInitialized?.Invoke();
        }

        public void ClearMap()
        {
            int width = Map.Width;
            int height = Map.Height;
            
            if (Map)
            {
                Destroy(Map.gameObject);
            }
            
            Map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            Map.Initialize(width, height);
            MapInitialized?.Invoke();
        }

        public IEnumerator LoadMap(Uri mapUri)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(mapUri);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
                webRequest.Dispose();
                yield break;
            }
            
            Debug.Log("Map downloaded, checking if compressed");
            string requestText = webRequest.downloadHandler.text;
            webRequest.Dispose();
            try
            {
                byte[] requestBytes = Convert.FromBase64String(requestText);
                byte[] decompressedBytes = DecompressGzip(requestBytes);
                requestText = Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length);
                Debug.Log("Compressed map, decompressed");
            }
            catch
            {
                Debug.Log("Not compressed map");
            }

            LoadMap(requestText);
        }
        
        public void LoadMap(string mapString)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(mapString);
            if (Map)
            {
                GameObject oldMapObject = Map.gameObject;
                oldMapObject.SetActive(false);
                Destroy(oldMapObject);
            }
            
            Map = _instantiator.InstantiateComponentOnNewGameObject<Map>("Map");
            Map.Initialize(doc);
            MapInitialized?.Invoke();
        }
        
        private byte[] DecompressGzip(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private void CheckUpdater(AbstractUpdater updater, Tab tab)
        {
            updater.gameObject.SetActive(updater.TargetTab == tab);
        }
        
        private void OnTabChange(Tab tab)
        { 
            foreach (AbstractUpdater updater in _updaters)
            {
                CheckUpdater(updater, tab);
            }

            if (Map != null)
            {
                Map.RenderGrid = LayoutManager.Instance.CurrentTab != Tab.Menu;
            }
        }

        public void OnDecorationsVisibilityChange(bool enable)
        {
            RenderDecorations = enable;
            RenderSettingsChanged?.Invoke();
        }

        public void OnTreeVisibilityChange(bool enable)
        {
            RenderTrees = enable;
            RenderSettingsChanged?.Invoke();
        }

        public void OnBushVisibilityChange(bool enable)
        {
            RenderBushes = enable;
            RenderSettingsChanged?.Invoke();
        }

        public void OnShipsVisibilityChange(bool enable)
        {
            RenderShips = enable;
            RenderSettingsChanged?.Invoke();
        }
    }
}