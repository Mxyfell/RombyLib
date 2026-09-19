using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppInterop.Runtime.Injection;
using Steamworks;
using UnityEngine;


namespace RombyLib
{
    public class Workshop
    {
        public const uint AppId = 4238990;

        private static readonly List<WorkshopItem> _items = new List<WorkshopItem>();
        private static readonly List<DownloadTask> _activeDownloads = new List<DownloadTask>();
        private static bool _isInitialized;

        public static IReadOnlyList<WorkshopItem> Items => _items;
        public static IEnumerable<WorkshopItem> InstalledItems => _items.Where(i => i.IsInstalled);
        public static IEnumerable<WorkshopItem> NotInstalledItems => _items.Where(i => !i.IsInstalled);

        public static void Init()
        {
            if(_isInitialized) return;

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<WorkshopRunner>();
                var runnerObj = new GameObject("WorkshopRunner");
                runnerObj.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(runnerObj);
                runnerObj.AddComponent<WorkshopRunner>();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Workshop] Runner initialization error: {ex.Message}");
            }
        }
        public static void Refresh()
        {
            _items.Clear();

            if (!SteamAPI.IsSteamRunning())
            {
                Debug.LogWarning("[RombyLib.Workshop] Steam API not active");
                return;
            }

            uint count = SteamUGC.GetNumSubscribedItems();
            if (count == 0)
            {
                Debug.Log("[RombyLib] No workshop subscribes");
                return;
            }

            var ItemIds = new PublishedFileId_t[count];
            uint actualCount = SteamUGC.GetSubscribedItems(ItemIds, count);

            for (int i = 0; i < actualCount; i++)
            {
                var fieldId = ItemIds[i];
                ulong rawId = fieldId.m_PublishedFileId;

                var item = new WorkshopItem { Id = rawId };
                UpdateItemStatus(item);
                _items.Add(item);
            }
        }
        public static void DownloadItem(ulong id, Action<WorkshopItem> onCompleted = null, Action<float> onProgress = null)
        {
            if (!SteamAPI.IsSteamRunning()) return;
            if (!_isInitialized) Init();

            var fieldId = new PublishedFileId_t(id);
            var item = GetById(id) ?? new WorkshopItem { Id = id };
            if (!_items.Contains(item)) _items.Add(item);

            SteamUGC.SubscribeItem(fieldId);
            bool started = SteamUGC.DownloadItem(fieldId, bHighPriority: true);

            _activeDownloads.Add(new DownloadTask
            {
                Id = id,
                Item = item,
                OnCompleted = onCompleted,
                OnProgress = onProgress
            });
        }
        public static WorkshopItem GetById(ulong id) => _items.FirstOrDefault(x => x.Id == id);

        internal static void UpdateDownloads()
        {
            if (_activeDownloads.Count == 0) return;

            for (int i = _activeDownloads.Count - 1; i >= 0; i--)
            {
                var task = _activeDownloads[i];
                var fileId = new PublishedFileId_t(task.Id);

                uint state = SteamUGC.GetItemState(fileId);
                bool isInstalled = (state & (uint)EItemState.k_EItemStateInstalled) != 0;
                bool isDownloading = (state & (uint)EItemState.k_EItemStateDownloading) != 0;
                bool isPending = (state & (uint)EItemState.k_EItemStateDownloadPending) != 0;


                if (SteamUGC.GetItemDownloadInfo(fileId, out ulong downloaded, out ulong total) && total > 0)
                {
                    float progress = (float)downloaded / total;
                    task.Item.DownloadProgress = progress;
                    task.OnProgress?.Invoke(progress);
                }

                if (isInstalled && !isDownloading && !isPending)
                {
                    task.Item.DownloadProgress = 1f;
                    UpdateItemStatus(task.Item);

                    _activeDownloads.RemoveAt(i);

                    try
                    {
                        task.OnCompleted?.Invoke(task.Item);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RombyLib.Workshop] Error in onCompleted: {ex}");
                    }
                }
            }
        }
        private static void UpdateItemStatus(WorkshopItem item)
        {
            var fileId = new PublishedFileId_t(item.Id);
            uint itemState = SteamUGC.GetItemState(fileId);

            item.IsInstalled = (itemState & (uint)EItemState.k_EItemStateInstalled) != 0;
            item.IsDownloading = (itemState & (uint)EItemState.k_EItemStateDownloading) != 0;

            if (item.IsInstalled)
            {
                if (SteamUGC.GetItemInstallInfo(fileId, out ulong _, out string folder, 1024, out uint _))
                {
                    item.InstallPath = folder;
                    ParseFolder(item, folder);
                }
            }
        }
        private static void ParseFolder(WorkshopItem item, string folder)
        {
            if (!Directory.Exists(folder)) return;

            string iconPath = Path.Combine(folder, "icon.png");
            if (File.Exists(iconPath)) item.IconPath = iconPath;

            string packJson = Path.Combine(folder, "pack.json");
            if (File.Exists(packJson))
            {
                try
                {
                    string json = File.ReadAllText(packJson);
                    var match = Regex.Match(json, "\"(?:name|title)\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (match.Success) item.Title = match.Groups[1].Value;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(item.Title) || item.Title == "Unknown")
            {
                item.Title = $"Skin_{item.Id}";
            }

            item.Elements.Clear();
            var subDirs = Directory.GetDirectories(folder);
            foreach (var dir in subDirs)
            {
                string elemName = Path.GetFileName(dir);
                var elem = new WorkshopElement { Name = elemName, FolderPath = dir };

                var pngFiles = Directory.GetFiles(dir, "*.png");
                Array.Sort(pngFiles, StringComparer.OrdinalIgnoreCase);

                foreach (var file in pngFiles)
                {
                    elem.TextureFiles.Add(file);
                }

                item.Elements[elemName] = elem;
            }
        }
        private class DownloadTask
        {
            public ulong Id;
            public WorkshopItem Item;
            public Action<WorkshopItem> OnCompleted;
            public Action<float> OnProgress;
        }

        public class WorkshopRunner : MonoBehaviour
        {
            public WorkshopRunner(IntPtr ptr) : base(ptr) { }
            private void Update() => Workshop.UpdateDownloads();
        }
    }
    public class WorkshopItem
    {
        public ulong Id { get; internal set; }
        public string Title { get; internal set; } = "Unknown";
        public string InstallPath { get; internal set; }
        public string IconPath { get; internal set; }
        public bool IsInstalled { get; internal set; }
        public bool IsDownloading { get; internal set; }
        public float DownloadProgress { get; internal set; }
        public bool HasIcon => !string.IsNullOrEmpty(IconPath) && File.Exists(IconPath);

        public Dictionary<string, WorkshopElement> Elements { get; } =
            new Dictionary<string, WorkshopElement>(StringComparer.OrdinalIgnoreCase);

        public bool HasElement(string name) => Elements.ContainsKey(name);
        public WorkshopElement GetElement(string name) => Elements.TryGetValue(name, out var elem) ? elem : null;
        
        public void Download(Action<WorkshopItem> onCompleted = null, Action<float> onProgress = null)
        {
            Workshop.DownloadItem(Id, onCompleted, onProgress);
        }
    }
    public class WorkshopElement
    {
        public string Name { get; internal set; }
        public string FolderPath { get; internal set; }
        public List<string> TextureFiles { get; } = new List<string>();
        public string MainTexturePath => TextureFiles.Count > 0 ? TextureFiles[0] : null;
    }
}
