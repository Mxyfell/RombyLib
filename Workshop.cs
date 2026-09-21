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
    /// <summary>
    /// Handles Steam Workshop integrations, skin package scanning, downloads, and textures.
    /// </summary>
    public class Workshop
    {
        /// <summary>Steam Application ID associated with the game workshop.</summary>
        public const uint AppId = 4238990;

        private static readonly List<WorkshopItem> _items = new List<WorkshopItem>();
        private static readonly List<DownloadTask> _activeDownloads = new List<DownloadTask>();
        private static bool _isInitialized;

        /// <summary>All subscribed workshop items found in the library.</summary>
        public static IReadOnlyList<WorkshopItem> Items => _items;

        /// <summary>Filtered collection of fully downloaded and installed workshop items.</summary>
        public static IEnumerable<WorkshopItem> InstalledItems => _items.Where(i => i.IsInstalled);

        /// <summary>Filtered collection of subscribed items that are not installed on disk.</summary>
        public static IEnumerable<WorkshopItem> NotInstalledItems => _items.Where(i => !i.IsInstalled);

        /// <summary>
        /// Registers background update runner into Unity Il2Cpp engine loop.
        /// </summary>
        public static void Init()
        {
            if (_isInitialized) return;

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<WorkshopRunner>();
                var runnerObj = new GameObject("WorkshopRunner");
                runnerObj.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(runnerObj);
                runnerObj.AddComponent<WorkshopRunner>();

                _isInitialized = true;
                Debug.Log("[RombyLib.Workshop] Initialized Workshop runner.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.Workshop] Failed to initialize WorkshopRunner: {ex}");
            }
        }

        /// <summary>
        /// Queries Steam UGC for all subscribed items and refreshes their metadata and directories.
        /// </summary>
        public static void Refresh()
        {
            _items.Clear();

            if (!SteamAPI.IsSteamRunning())
            {
                Debug.LogWarning("[RombyLib.Workshop] Refresh skipped: Steam API is not running.");
                return;
            }

            uint count = SteamUGC.GetNumSubscribedItems();
            if (count == 0)
            {
                Debug.LogWarning("[RombyLib.Workshop] No subscribed workshop items found.");
                return;
            }

            var itemIds = new PublishedFileId_t[count];
            uint actualCount = SteamUGC.GetSubscribedItems(itemIds, count);

            for (int i = 0; i < actualCount; i++)
            {
                var fieldId = itemIds[i];
                ulong rawId = fieldId.m_PublishedFileId;

                var item = new WorkshopItem { Id = rawId };
                UpdateItemStatus(item);
                _items.Add(item);
            }

            Debug.Log($"[RombyLib.Workshop] Refreshed {actualCount} subscribed workshop items.");
        }

        /// <summary>
        /// Subscribes to and downloads a workshop item asynchronously.
        /// </summary>
        /// <param name="id">Published file ID from Steam.</param>
        /// <param name="onCompleted">Callback triggered once download finishes and files are parsed.</param>
        /// <param name="onProgress">Callback reporting download progress (0.0 to 1.0).</param>
        public static void DownloadItem(ulong id, Action<WorkshopItem> onCompleted = null, Action<float> onProgress = null)
        {
            if (!SteamAPI.IsSteamRunning())
            {
                Debug.LogError($"[RombyLib.Workshop] Cannot download item {id}: Steam API is not running.");
                return;
            }

            if (!_isInitialized) Init();

            var fieldId = new PublishedFileId_t(id);
            var item = GetById(id) ?? new WorkshopItem { Id = id };
            if (!_items.Contains(item)) _items.Add(item);

            SteamUGC.SubscribeItem(fieldId);
            bool started = SteamUGC.DownloadItem(fieldId, bHighPriority: true);

            if (!started)
            {
                Debug.LogWarning($"[RombyLib.Workshop] SteamUGC.DownloadItem returned false for item ID: {id}.");
            }
            else
            {
                Debug.Log($"[RombyLib.Workshop] Started downloading workshop item: {id}.");
            }

            _activeDownloads.Add(new DownloadTask
            {
                Id = id,
                Item = item,
                OnCompleted = onCompleted,
                OnProgress = onProgress
            });
        }

        /// <summary>
        /// Finds a workshop item by its ID from the cached items list.
        /// </summary>
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
                    Debug.Log($"[RombyLib.Workshop] Successfully downloaded workshop item {task.Id} ('{task.Item.Title}').");

                    try
                    {
                        task.OnCompleted?.Invoke(task.Item);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RombyLib.Workshop] Error in OnCompleted callback for item {task.Id}: {ex}");
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
                else
                {
                    Debug.LogWarning($"[RombyLib.Workshop] Item {item.Id} is marked as installed, but Steam failed to return its install directory.");
                }
            }
        }

        private static void ParseFolder(WorkshopItem item, string folder)
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"[RombyLib.Workshop] Install folder not found for item {item.Id}: '{folder}'");
                return;
            }

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
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Workshop] Failed to read or parse pack.json for item {item.Id}: {ex.Message}");
                }
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

    /// <summary>
    /// Represents a downloaded or subscribed item from Steam Workshop.
    /// </summary>
    public class WorkshopItem
    {
        /// <summary>Steam Workshop Published File ID.</summary>
        public ulong Id { get; internal set; }

        /// <summary>Display name defined in pack.json or fallback name.</summary>
        public string Title { get; internal set; } = "Unknown";

        /// <summary>Absolute path to item folder on local drive.</summary>
        public string InstallPath { get; internal set; }

        /// <summary>Absolute path to icon.png if present.</summary>
        public string IconPath { get; internal set; }

        /// <summary>Whether files are downloaded and available on disk.</summary>
        public bool IsInstalled { get; internal set; }

        /// <summary>Whether this item is currently downloading.</summary>
        public bool IsDownloading { get; internal set; }

        /// <summary>Download progress ratio from 0.0 to 1.0.</summary>
        public float DownloadProgress { get; internal set; }

        /// <summary>Checks whether a valid preview icon exists for this item.</summary>
        public bool HasIcon => !string.IsNullOrEmpty(IconPath) && File.Exists(IconPath);

        /// <summary>Dictionary of folder elements/parts contained in this skin pack.</summary>
        public Dictionary<string, WorkshopElement> Elements { get; } =
            new Dictionary<string, WorkshopElement>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Checks whether a part/element folder exists by name.</summary>
        public bool HasElement(string name) => Elements.ContainsKey(name);

        /// <summary>Retrieves a part/element folder by name.</summary>
        public WorkshopElement GetElement(string name) => Elements.TryGetValue(name, out var elem) ? elem : null;

        /// <summary>Starts download for this specific item.</summary>
        public void Download(Action<WorkshopItem> onCompleted = null, Action<float> onProgress = null)
        {
            Workshop.DownloadItem(Id, onCompleted, onProgress);
        }
    }

    /// <summary>
    /// Represents a sub-folder inside a workshop item containing texture sprites (e.g., animations or layers).
    /// </summary>
    public class WorkshopElement
    {
        /// <summary>Name of the element folder.</summary>
        public string Name { get; internal set; }

        /// <summary>Full folder path.</summary>
        public string FolderPath { get; internal set; }

        /// <summary>List of PNG image paths found inside this folder.</summary>
        public List<string> TextureFiles { get; } = new List<string>();

        /// <summary>Path to the first texture file in folder, or null if empty.</summary>
        public string MainTexturePath => TextureFiles.Count > 0 ? TextureFiles[0] : null;
    }
}