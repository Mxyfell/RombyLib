using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RombyLib
{
    #region Attributes

    /// <summary>
    /// Marks a public static field or property as a configurable mod setting.
    /// RombyLib automatically discovers it, generates an in-game UI control in the Settings menu ("УЛЬТРАМОД"),
    /// and persists its value to disk in BepInEx/config/mods/{modId}.json.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ManageAttribute : Attribute
    {
        /// <summary>User-friendly display name of the setting shown in the UI.</summary>
        public string Name { get; set; }

        /// <summary>Helpful description or tooltip displayed underneath the setting title.</summary>
        public string Description { get; set; }

        /// <summary>Category or section grouping for this setting (e.g. "Геймплей", "Интерфейс", "Читы").</summary>
        public string Category { get; set; }

        /// <summary>Minimum value for numeric sliders.</summary>
        public float Min { get; set; } = 0f;

        /// <summary>Maximum value for numeric sliders.</summary>
        public float Max { get; set; } = 100f;

        /// <summary>Step size for numeric adjustments.</summary>
        public float Step { get; set; } = 1f;

        /// <summary>Format string for displaying current numeric value (e.g. "{0:0.0}x", "{0}%", "{0}").</summary>
        public string Format { get; set; } = "{0}";

        /// <summary>Sort order priority within category (lower values appear first).</summary>
        public int Order { get; set; } = 0;

        /// <summary>Creates a new declarative mod setting attribute.</summary>
        public ManageAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Alias for <see cref="ManageAttribute"/> to define in-game mod settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ModSettingAttribute : ManageAttribute
    {
        /// <summary>Creates a new mod setting attribute.</summary>
        public ModSettingAttribute(string name = null, string description = null) : base(name, description) { }
    }

    /// <summary>
    /// Marks a public static parameterless method as an in-game executable action button.
    /// Appears in the mod's settings panel in the Settings menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ModActionAttribute : Attribute
    {
        /// <summary>Label text displayed on the action button.</summary>
        public string Name { get; set; }

        /// <summary>Description explaining what this action does.</summary>
        public string Description { get; set; }

        /// <summary>Optional category grouping.</summary>
        public string Category { get; set; }

        /// <summary>Sort order priority.</summary>
        public int Order { get; set; } = 0;

        /// <summary>Creates a new mod action button attribute.</summary>
        public ModActionAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Marks a method to be automatically invoked when any setting in the mod is modified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OnSettingChangedAttribute : Attribute
    {
    }

    #endregion

    #region Data Models

    /// <summary>
    /// Represents an individual registered mod setting bound to a live C# field or property via reflection.
    /// </summary>
    public class ModSettingEntry
    {
        /// <summary>Unique key identifier for config persistence (defaults to member name).</summary>
        public string Key { get; }

        /// <summary>Display name shown to the user.</summary>
        public string DisplayName { get; }

        /// <summary>Tooltip or explanatory text.</summary>
        public string Description { get; }

        /// <summary>Category grouping name.</summary>
        public string Category { get; }

        /// <summary>Underlying CLR type (bool, float, int, string, Enum).</summary>
        public Type ValueType { get; }

        /// <summary>Initial default value captured upon mod registration.</summary>
        public object DefaultValue { get; }

        /// <summary>Configuration metadata attribute.</summary>
        public ManageAttribute Attribute { get; }

        /// <summary>Target member reflection info.</summary>
        public MemberInfo Member { get; }

        /// <summary>Reference to the owning mod definition.</summary>
        public ModDefinition Mod { get; internal set; }

        internal ModSettingEntry(MemberInfo member, ManageAttribute attr)
        {
            Member = member;
            Attribute = attr;
            Key = member.Name;
            DisplayName = !string.IsNullOrEmpty(attr.Name) ? attr.Name : member.Name;
            Description = attr.Description ?? string.Empty;
            Category = !string.IsNullOrEmpty(attr.Category) ? attr.Category : "Общее";

            if (member is FieldInfo fi)
            {
                ValueType = fi.FieldType;
                DefaultValue = fi.GetValue(null);
            }
            else if (member is PropertyInfo pi)
            {
                ValueType = pi.PropertyType;
                DefaultValue = pi.GetValue(null);
            }
            else
            {
                throw new ArgumentException("Member must be a field or property.");
            }
        }

        /// <summary>Reads the current live value of this setting.</summary>
        public object GetValue()
        {
            try
            {
                if (Member is FieldInfo fi) return fi.GetValue(null);
                if (Member is PropertyInfo pi) return pi.GetValue(null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] Failed to get value for {Key}: {ex.Message}");
            }
            return DefaultValue;
        }

        /// <summary>Updates the live value of this setting, notifies callbacks, and persists to disk.</summary>
        public void SetValue(object value, bool notify = true)
        {
            try
            {
                object converted = ConvertValue(value, ValueType);
                if (Member is FieldInfo fi) fi.SetValue(null, converted);
                else if (Member is PropertyInfo pi) pi.SetValue(null, converted);

                if (notify && Mod != null)
                {
                    Mod.NotifySettingChanged(this);
                    Mod.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] Failed to set value for {Key}: {ex.Message}");
            }
        }

        /// <summary>Resets this setting back to its original default value.</summary>
        public void ResetToDefault()
        {
            SetValue(DefaultValue, notify: true);
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            if (targetType == typeof(float)) return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return Convert.ToBoolean(value);
            if (targetType == typeof(string)) return value.ToString();
            if (targetType.IsEnum)
            {
                if (value is string s) return Enum.Parse(targetType, s, true);
                return Enum.ToObject(targetType, value);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Represents an executable action button declared via <see cref="ModActionAttribute"/>.
    /// </summary>
    public class ModActionEntry
    {
        /// <summary>Button label.</summary>
        public string Name { get; }

        /// <summary>Action description or status text.</summary>
        public string Description { get; }

        /// <summary>Category grouping name.</summary>
        public string Category { get; }

        /// <summary>Target method to invoke.</summary>
        public MethodInfo Method { get; }

        /// <summary>Sort order priority.</summary>
        public int Order { get; }

        internal ModActionEntry(MethodInfo method, ModActionAttribute attr)
        {
            Method = method;
            Name = !string.IsNullOrEmpty(attr.Name) ? attr.Name : method.Name;
            Description = attr.Description ?? string.Empty;
            Category = !string.IsNullOrEmpty(attr.Category) ? attr.Category : "Действия";
            Order = attr.Order;
        }

        /// <summary>Invokes the action method safely with exception handling.</summary>
        public bool Invoke()
        {
            try
            {
                Method.Invoke(null, null);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] Error executing mod action '{Name}': {ex}");
                return false;
            }
        }
    }

    /// <summary>
    /// Metadata, settings collection, and persistence controller for a registered mod.
    /// </summary>
    public class ModDefinition
    {
        /// <summary>Unique identifier (GUID or namespace).</summary>
        public string Id { get; internal set; }

        /// <summary>Human-readable display name of the mod.</summary>
        public string Name { get; internal set; }

        /// <summary>Version string (e.g. "1.0.0").</summary>
        public string Version { get; internal set; }

        /// <summary>Author or team credit.</summary>
        public string Author { get; internal set; }

        /// <summary>General mod description.</summary>
        public string Description { get; internal set; }

        /// <summary>Type hosting the static settings fields.</summary>
        public Type TargetType { get; }

        /// <summary>List of all discovered configurable settings.</summary>
        public List<ModSettingEntry> Settings { get; } = new List<ModSettingEntry>();

        /// <summary>List of all discovered action buttons.</summary>
        public List<ModActionEntry> Actions { get; } = new List<ModActionEntry>();

        /// <summary>Methods marked with [OnSettingChanged] called when values mutate.</summary>
        public List<MethodInfo> ChangeCallbacks { get; } = new List<MethodInfo>();

        /// <summary>File path to the persistent JSON config on disk.</summary>
        public string ConfigFilePath { get; }

        internal ModDefinition(string id, string name, string version, string author, string description, Type targetType)
        {
            Id = id;
            Name = !string.IsNullOrEmpty(name) ? name : id;
            Version = version ?? "1.0.0";
            Author = author ?? "Unknown";
            Description = description ?? string.Empty;
            TargetType = targetType;

            string configDir = Path.Combine(Paths.ConfigPath, "mods");
            try
            {
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);
            }
            catch { }

            ConfigFilePath = Path.Combine(configDir, $"{id}.json");
        }

        internal void NotifySettingChanged(ModSettingEntry entry)
        {
            foreach (var callback in ChangeCallbacks)
            {
                try
                {
                    var pars = callback.GetParameters();
                    if (pars.Length == 0) callback.Invoke(null, null);
                    else if (pars.Length == 1 && pars[0].ParameterType == typeof(ModSettingEntry))
                        callback.Invoke(null, new object[] { entry });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.ModManager] Callback error in {callback.Name}: {ex.Message}");
                }
            }

            ModManager.TriggerSettingChanged(this, entry);
        }

        /// <summary>Saves all settings of this mod to disk as clean JSON.</summary>
        public void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                for (int i = 0; i < Settings.Count; i++)
                {
                    var s = Settings[i];
                    var val = s.GetValue();
                    string valStr;

                    if (val is bool b) valStr = b ? "true" : "false";
                    else if (val is float f) valStr = f.ToString("G", CultureInfo.InvariantCulture);
                    else if (val is double d) valStr = d.ToString("G", CultureInfo.InvariantCulture);
                    else if (val is int n) valStr = n.ToString(CultureInfo.InvariantCulture);
                    else if (val is Enum e) valStr = $"\"{e}\"";
                    else valStr = $"\"{val?.ToString()?.Replace("\"", "\\\"")}\"";

                    sb.Append($"  \"{s.Key}\": {valStr}");
                    if (i < Settings.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("}");

                File.WriteAllText(ConfigFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.ModManager] Failed to save config for {Id}: {ex.Message}");
            }
        }

        /// <summary>Loads settings from disk JSON, applying them to live fields.</summary>
        public void Load()
        {
            if (!File.Exists(ConfigFilePath)) return;

            try
            {
                string text = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                var matches = Regex.Matches(text, @"""(?<key>[^""]+)""\s*:\s*(?<val>[^,\r\n}]+)");

                foreach (Match m in matches)
                {
                    string key = m.Groups["key"].Value;
                    string rawVal = m.Groups["val"].Value.Trim();
                    if (rawVal.StartsWith("\"") && rawVal.EndsWith("\""))
                        rawVal = rawVal.Substring(1, rawVal.Length - 2);

                    var setting = Settings.Find(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (setting != null)
                    {
                        object parsed = ParseValue(rawVal, setting.ValueType);
                        if (parsed != null)
                        {
                            setting.SetValue(parsed, notify: false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.ModManager] Failed to load config for {Id}: {ex.Message}");
            }
        }

        /// <summary>Resets all settings in this mod to their initial default values.</summary>
        public void ResetAll()
        {
            foreach (var setting in Settings)
            {
                setting.ResetToDefault();
            }
            Save();
        }

        private static object ParseValue(string raw, Type type)
        {
            try
            {
                if (type == typeof(bool))
                {
                    if (bool.TryParse(raw, out bool b)) return b;
                    return raw == "1";
                }
                if (type == typeof(float))
                {
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
                }
                if (type == typeof(int))
                {
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return i;
                }
                if (type == typeof(double))
                {
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                }
                if (type.IsEnum)
                {
                    return Enum.Parse(type, raw, true);
                }
                if (type == typeof(string)) return raw;
            }
            catch { }
            return null;
        }
    }

    #endregion

    #region ModManager Controller

    /// <summary>
    /// Core Mod Manager orchestrator ("УЛЬТРАМОД") providing reflection discovery, config persistence,
    /// and seamless native Settings UI injection.
    /// </summary>
    public static class ModManager
    {
        private static readonly Dictionary<string, ModDefinition> _mods = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<Type> _registeredTypes = new HashSet<Type>();
        private static bool _isInitialized;

        /// <summary>Triggered whenever any mod setting changes.</summary>
        public static event Action<ModDefinition, ModSettingEntry> OnSettingChanged;

        /// <summary>Gets all currently registered mod definitions.</summary>
        public static IReadOnlyList<ModDefinition> AllMods => new List<ModDefinition>(_mods.Values);

        /// <summary>Initializes ModManager hooks, IL2CPP registration, and scans loaded mods.</summary>
        public static void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                // Register custom hook into Unity IL2CPP runtime
                ClassInjector.RegisterTypeInIl2Cpp<ModManagerSettingsHook>();

                // Apply Harmony patches to SettingsManager
                var harmony = new Harmony(Hook.ModGUID + ".modmanager");
                harmony.CreateClassProcessor(typeof(SettingsManagerOnEnablePatch)).Patch();
                harmony.CreateClassProcessor(typeof(SettingsManagerShowSectionPatch)).Patch();
                harmony.CreateClassProcessor(typeof(SettingsManagerHeightPatch)).Patch();

                Debug.Log("[RombyLib.ModManager] Injected SettingsManager patches successfully.");

                // Auto-scan loaded assemblies
                ScanLoadedAssemblies();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] Initialization failed: {ex}");
            }
        }

        /// <summary>
        /// Registers a mod settings container class explicitly.
        /// </summary>
        public static ModDefinition Register<T>(string modId = null, string modName = null, string version = null, string author = null, string description = null)
        {
            return Register(typeof(T), modId, modName, version, author, description);
        }

        /// <summary>
        /// Registers a mod settings container class explicitly by <see cref="Type"/>.
        /// </summary>
        public static ModDefinition Register(Type targetType, string modId = null, string modName = null, string version = null, string author = null, string description = null)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));

            // 1. Check if a ModDefinition already exists for this exact targetType
            ModDefinition existing = null;
            foreach (var kvp in _mods)
            {
                if (kvp.Value.TargetType == targetType)
                {
                    existing = kvp.Value;
                    break;
                }
            }

            if (existing != null)
            {
                // If a new modId is provided that differs from the existing one, update dictionary key
                if (!string.IsNullOrEmpty(modId) && existing.Id != modId)
                {
                    _mods.Remove(existing.Id);
                    existing.Id = modId;
                    _mods[modId] = existing;
                }
                if (!string.IsNullOrEmpty(modName)) existing.Name = modName;
                if (!string.IsNullOrEmpty(version)) existing.Version = version;
                if (!string.IsNullOrEmpty(author)) existing.Author = author;
                if (!string.IsNullOrEmpty(description)) existing.Description = description;
                return existing;
            }

            // 2. Derive unique modId if not provided
            if (string.IsNullOrEmpty(modId))
            {
                var bepAttr = targetType.GetCustomAttribute<BepInPlugin>() ?? targetType.Assembly.GetCustomAttribute<BepInPlugin>();
                if (bepAttr != null)
                {
                    if (targetType.GetCustomAttribute<BepInPlugin>() != null)
                    {
                        modId = bepAttr.GUID;
                    }
                    else
                    {
                        modId = $"{bepAttr.GUID}.{targetType.Name}";
                    }
                }
                else
                {
                    modId = targetType.FullName ?? targetType.Name;
                }
            }

            // 3. Ensure dictionary key uniqueness
            if (_mods.ContainsKey(modId))
            {
                modId = $"{modId}.{targetType.Name}";
            }

            // 4. Derive modName if not provided
            if (string.IsNullOrEmpty(modName))
            {
                if (targetType.Name == "Test")
                {
                    modName = "RombyLib Core";
                }
                else
                {
                    var bepAttr = targetType.GetCustomAttribute<BepInPlugin>();
                    if (bepAttr != null && !string.IsNullOrEmpty(bepAttr.Name))
                    {
                        modName = bepAttr.Name;
                    }
                    else
                    {
                        modName = System.Text.RegularExpressions.Regex.Replace(targetType.Name, "(\\B[A-Z])", " $1");
                    }
                }
            }

            // 5. Derive version if not provided
            if (string.IsNullOrEmpty(version))
            {
                var bepAttr = targetType.GetCustomAttribute<BepInPlugin>() ?? targetType.Assembly.GetCustomAttribute<BepInPlugin>();
                version = bepAttr != null ? bepAttr.GetType().GetProperty("Version")?.GetValue(bepAttr)?.ToString() : targetType.Assembly.GetName().Version?.ToString();
                if (string.IsNullOrEmpty(version)) version = "1.0.0";
            }

            var mod = new ModDefinition(modId, modName, version, author, description, targetType);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            foreach (var f in targetType.GetFields(flags))
            {
                var attr = f.GetCustomAttribute<ManageAttribute>();
                if (attr != null)
                {
                    var entry = new ModSettingEntry(f, attr) { Mod = mod };
                    mod.Settings.Add(entry);
                }
            }

            foreach (var p in targetType.GetProperties(flags))
            {
                var attr = p.GetCustomAttribute<ManageAttribute>();
                if (attr != null && p.CanRead && p.CanWrite)
                {
                    var entry = new ModSettingEntry(p, attr) { Mod = mod };
                    mod.Settings.Add(entry);
                }
            }

            foreach (var m in targetType.GetMethods(flags))
            {
                var actionAttr = m.GetCustomAttribute<ModActionAttribute>();
                if (actionAttr != null && m.GetParameters().Length == 0)
                {
                    mod.Actions.Add(new ModActionEntry(m, actionAttr));
                }

                if (m.GetCustomAttribute<OnSettingChangedAttribute>() != null)
                {
                    mod.ChangeCallbacks.Add(m);
                }
            }

            mod.Settings.Sort((a, b) => a.Attribute.Order.CompareTo(b.Attribute.Order));
            mod.Actions.Sort((a, b) => a.Order.CompareTo(b.Order));

            mod.Load();

            _mods[modId] = mod;
            _registeredTypes.Add(targetType);

            Debug.Log($"[RombyLib.ModManager] Registered mod '{mod.Name}' (ID: {mod.Id}) with {mod.Settings.Count} settings and {mod.Actions.Count} actions.");
            return mod;
        }

        /// <summary>
        /// Automatically scans all loaded assemblies for classes declaring [Manage] or [ModSetting] fields.
        /// </summary>
        public static void ScanLoadedAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        string asmName = asm.GetName().Name;
                        if (asmName.StartsWith("System") || asmName.StartsWith("UnityEngine") || asmName.StartsWith("mscorlib") || asmName.StartsWith("Il2Cpp"))
                            continue;

                        foreach (var type in asm.GetTypes())
                        {
                            if (_registeredTypes.Contains(type)) continue;

                            bool hasManage = false;
                            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                            foreach (var f in type.GetFields(flags))
                            {
                                if (f.GetCustomAttribute<ManageAttribute>() != null) { hasManage = true; break; }
                            }
                            if (!hasManage)
                            {
                                foreach (var p in type.GetProperties(flags))
                                {
                                    if (p.GetCustomAttribute<ManageAttribute>() != null) { hasManage = true; break; }
                                }
                            }

                            if (hasManage)
                            {
                                Register(type);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.ModManager] Assembly scan error: {ex.Message}");
            }
        }

        /// <summary>Gets a registered mod by identifier, or null if not found.</summary>
        public static ModDefinition GetMod(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return null;
            _mods.TryGetValue(modId, out var mod);
            return mod;
        }

        /// <summary>Saves configs of all registered mods to disk.</summary>
        public static void SaveAll()
        {
            foreach (var mod in _mods.Values)
            {
                mod.Save();
            }
        }

        internal static void TriggerSettingChanged(ModDefinition mod, ModSettingEntry entry)
        {
            try
            {
                OnSettingChanged?.Invoke(mod, entry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] Error in global OnSettingChanged listener: {ex}");
            }
        }
    }

    #endregion

    #region In-Game Settings UI & Integration

    /// <summary>
    /// Custom MonoBehaviour registered into IL2CPP runtime and attached to active SettingsManager instances.
    /// Manages the injected [Моды] tab and custom settings panel lifecycle with dynamic background height.
    /// </summary>
    public class ModManagerSettingsHook : MonoBehaviour
    {
        public SettingsManager SettingsManager;
        public GameObject ButtonMods;
        public TextMeshProUGUI ButtonModsText;
        public GameObject ModsSection;
        public CanvasGroup ModsCanvasGroup;
        public Image ButtonImage;
        public ModDefinition SelectedMod;
        public RectTransform SettingsContentContainer;
        public RectTransform ScrollViewRect;
        public TextMeshProUGUI ModSelectorText;
        public GameObject PrevModButton;
        public GameObject NextModButton;
        public int CurrentModIndex;
        public float CurrentTargetHeight = 767f;

        private void Update()
        {
            EnforceText();
            if (SettingsManager != null)
            {
                ModManagerUI.UpdateTabBackgroundWidth(SettingsManager);
            }
        }

        private void LateUpdate()
        {
            EnforceText();
            if (SettingsManager != null)
            {
                ModManagerUI.UpdateTabBackgroundWidth(SettingsManager);
            }
        }

        private void EnforceText()
        {
            if (ButtonModsText != null && ButtonModsText.text != "Mods")
            {
                ButtonModsText.text = "Mods";
                ButtonModsText.SetText("Mods");
            }
        }

        /// <summary>
        /// Calculates the ideal panel background height dynamically based on the current mod's categories, settings, and actions.
        /// </summary>
        public float CalculateTargetHeight()
        {
            var mods = ModManager.AllMods;
            if (mods.Count == 0) return 450f;

            int idx = Mathf.Clamp(CurrentModIndex, 0, mods.Count - 1);
            var mod = mods[idx];

            var catSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in mod.Settings)
            {
                catSet.Add(!string.IsNullOrEmpty(s.Category) ? s.Category : "General");
            }
            foreach (var a in mod.Actions)
            {
                catSet.Add(!string.IsNullOrEmpty(a.Category) ? a.Category : "Actions");
            }

            int catCount = catSet.Count;
            int settingCount = mod.Settings.Count;
            int actionCount = mod.Actions.Count;

            // Content Height Breakdown inside ScrollView:
            // - Category Headers: 38px each (32px + 6px spacing)
            // - Setting Rows: 50px each (44px + 6px spacing)
            // - Action Rows: 50px each (44px + 6px spacing)
            // - Bottom Action Bar: 56px (50px + 6px spacing)
            // - VerticalLayoutGroup padding: 19px (top 4 + bottom 15)
            float contentHeight = (catCount * 38f) + (settingCount * 50f) + (actionCount * 50f) + 56f + 19f;

            // Panel Layout Geometry:
            // - Header offset from Fondo top down to ScrollView: 221px + 40px for ModSelectorBar = 261px
            // - Comfortable bottom breathing room / padding: 35px
            float idealFondoHeight = 261f + contentHeight + 35f;

            // Clamp between comfortable min (450px) and max on-screen height (850px)
            return Mathf.Clamp(idealFondoHeight, 450f, 850f);
        }

        /// <summary>
        /// Updates the section background height and ScrollView dimensions with smooth animation matching native tabs.
        /// </summary>
        public void UpdateBackgroundHeight(bool instant = false)
        {
            if (!Romby.IsValid(SettingsManager) || !Romby.IsValid(SettingsManager.sectionBackground)) return;

            float newHeight = CalculateTargetHeight();
            CurrentTargetHeight = newHeight;

            // Compute exact content height for active mod
            var mods = ModManager.AllMods;
            float contentHeight = 580f;
            if (mods.Count > 0)
            {
                int idx = Mathf.Clamp(CurrentModIndex, 0, mods.Count - 1);
                var mod = mods[idx];
                var catSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in mod.Settings) catSet.Add(!string.IsNullOrEmpty(s.Category) ? s.Category : "General");
                foreach (var a in mod.Actions) catSet.Add(!string.IsNullOrEmpty(a.Category) ? a.Category : "Actions");
                contentHeight = (catSet.Count * 38f) + (mod.Settings.Count * 50f) + (mod.Actions.Count * 50f) + 56f + 19f;
            }

            if (ScrollViewRect != null)
            {
                // Usable vertical space in Fondo below header & selector bar (261px) leaving 30px bottom margin
                float maxScrollHeight = Mathf.Max(180f, newHeight - 261f - 30f);
                float targetScrollHeight = Mathf.Min(contentHeight + 6f, maxScrollHeight);
                ScrollViewRect.sizeDelta = new Vector2(800f, targetScrollHeight);
            }

            if (SettingsManager._heightTransitionCoroutine != null && SettingsManager._heightTransitionCoroutine.Pointer != IntPtr.Zero)
            {
                SettingsManager.StopCoroutine(SettingsManager._heightTransitionCoroutine);
                SettingsManager._heightTransitionCoroutine = null;
            }

            float targetWidth = ModManagerUI.GetTargetPanelWidth(SettingsManager);

            if (instant)
            {
                SettingsManager.sectionBackground.sizeDelta = new Vector2(targetWidth, newHeight);
            }
            else
            {
                SettingsManager.sectionBackground.sizeDelta = new Vector2(targetWidth, SettingsManager.sectionBackground.sizeDelta.y);
                SettingsManager._heightTransitionCoroutine = SettingsManager.StartCoroutine(SettingsManager.AnimateBackgroundHeight(newHeight));
            }
            ModManagerUI.UpdateTabBackgroundWidth(SettingsManager);
        }

        private void OnDestroy()
        {
            if (ModsSection != null)
            {
                Destroy(ModsSection);
            }
        }
    }

    /// <summary>
    /// Harmony patch targeting <see cref="SettingsManager.OnEnable"/> to inject the [Моды] tab and panel.
    /// </summary>
    [HarmonyPatch(typeof(SettingsManager), "OnEnable")]
    internal static class SettingsManagerOnEnablePatch
    {
        internal static void Postfix(SettingsManager __instance)
        {
            if (!Romby.IsValid(__instance)) return;
            ModManagerUI.EnsureInjected(__instance);
        }
    }

    /// <summary>
    /// Harmony patch targeting <see cref="SettingsManager.ShowSection"/> to cleanly hide the custom [Моды] section
    /// when the player navigates to vanilla sections (Graphics, Audio, Language, Others).
    /// </summary>
    [HarmonyPatch(typeof(SettingsManager), "ShowSection")]
    internal static class SettingsManagerShowSectionPatch
    {
        internal static void Postfix(SettingsManager __instance, GameObject section)
        {
            if (!Romby.IsValid(__instance)) return;
            ModManagerUI.OnVanillaSectionShown(__instance, section);
            ModManagerUI.UpdateTabBackgroundWidth(__instance);
        }
    }

    /// <summary>
    /// Harmony patch targeting <see cref="SettingsManager.ApplySectionBackgroundHeight"/> to dynamically animate the background
    /// height according to active mod content.
    /// </summary>
    [HarmonyPatch(typeof(SettingsManager), "ApplySectionBackgroundHeight")]
    internal static class SettingsManagerHeightPatch
    {
        internal static bool Prefix(SettingsManager __instance, GameObject section, bool instant)
        {
            if (!Romby.IsValid(__instance)) return true;
            ModManagerUI.UpdateTabBackgroundWidth(__instance);
            var hook = __instance.GetComponent<ModManagerSettingsHook>();
            if (hook != null && Romby.IsValid(hook.ModsSection) && section == hook.ModsSection)
            {
                hook.UpdateBackgroundHeight(instant);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// In-game UI builder and renderer for the "УЛЬТРАМОД" Mod Manager.
    /// Designed to be 100% indistinguishable from Rombykon's native settings aesthetics.
    /// </summary>
    public static class ModManagerUI
    {
        private static readonly Color TabNormalColor = new Color(0f, 0.384f, 0.502f, 1f); // #006280
        private static readonly Color TabActiveColor = new Color(0.12f, 0.65f, 0.85f, 1f); // Glowing active cyan
        private static readonly Color NativeDarkTeal = new Color(0f, 0.384f, 0.502f, 1f); // #006280 native checkbox/button box
        private static readonly Color NativeCyanCheck = new Color(0.714f, 0.949f, 1f, 1f); // #b6f2ff native cyan checkmark
        private static readonly Color SliderTrackColor = new Color(0.294f, 0.412f, 0.584f, 1f); // #4b6995 native slider track
        private static readonly Color SliderFillColor = new Color(0.600f, 0.906f, 1f, 1f); // #99e7ff native slider fill
        private static readonly Color TextWhite = new Color(0.88f, 0.96f, 1f, 1f); // Crisp bright ice-white

        private static TMP_FontAsset _nativeSettingsFont;

        /// <summary>Cached native settings font acquired directly from the game's tab and label components.</summary>
        public static TMP_FontAsset NativeSettingsFont
        {
            get
            {
                if (_nativeSettingsFont != null && _nativeSettingsFont.Pointer != IntPtr.Zero && !_nativeSettingsFont.WasCollected)
                    return _nativeSettingsFont;
                var gf = UI.GameFont;
                if (gf != null)
                {
                    _nativeSettingsFont = gf;
                    return _nativeSettingsFont;
                }
                return null;
            }
            internal set => _nativeSettingsFont = value;
        }

        /// <summary>Cached native button sprite from SettingsManager.</summary>
        public static Sprite NativeButtonSprite { get; set; }

        /// <summary>Cached native toggle sprite from SettingsManager.</summary>
        public static Sprite NativeToggleSprite { get; set; }

        /// <summary>Cached native slider sprite from SettingsManager.</summary>
        public static Sprite NativeSliderSprite { get; set; }

        /// <summary>Cached native cross checkmark sprite from SettingsManager (Icon_FlatWhite_Delete).</summary>
        public static Sprite NativeCheckmarkSprite { get; set; }

        /// <summary>
        /// Injects the [Моды] tab button and the ModsSection panel into the specified SettingsManager instance.
        /// </summary>
        public static void EnsureInjected(SettingsManager sm)
        {
            try
            {
                if (!Romby.IsValid(sm) || !Romby.IsValid(sm.btnSectionOthers)) return;

                // Cache native sprites if not yet acquired
                if (NativeButtonSprite == null && Romby.IsValid(sm.btnSectionOthers))
                {
                    var btnImg = sm.btnSectionOthers.GetComponent<Image>();
                    if (btnImg != null && btnImg.sprite != null)
                        NativeButtonSprite = btnImg.sprite;
                }

                if (NativeToggleSprite == null && Romby.IsValid(sm.disableCoopCursorLineToggle))
                {
                    var toggleImg = sm.disableCoopCursorLineToggle.GetComponentInChildren<Image>();
                    if (toggleImg != null && toggleImg.sprite != null)
                        NativeToggleSprite = toggleImg.sprite;
                }

                if (NativeSliderSprite == null && Romby.IsValid(sm.volumeGeneralSlider))
                {
                    var sliderImg = sm.volumeGeneralSlider.GetComponentInChildren<Image>();
                    if (sliderImg != null && sliderImg.sprite != null)
                        NativeSliderSprite = sliderImg.sprite;
                }

                if (NativeCheckmarkSprite == null)
                {
                    var allImgs = sm.GetComponentsInChildren<Image>(true);
                    foreach (var img in allImgs)
                    {
                        if (img != null && img.sprite != null && img.sprite.name.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            NativeCheckmarkSprite = img.sprite;
                            break;
                        }
                    }
                }

                // Acquire native settings font
                if (_nativeSettingsFont == null && Romby.IsValid(sm.btnSectionOthers))
                {
                    var tmp = sm.btnSectionOthers.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null && tmp.font != null)
                        _nativeSettingsFont = tmp.font;
                }
                if (_nativeSettingsFont == null && Romby.IsValid(sm.btnSectionGraphics))
                {
                    var tmp = sm.btnSectionGraphics.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null && tmp.font != null)
                        _nativeSettingsFont = tmp.font;
                }

                var hook = sm.GetComponent<ModManagerSettingsHook>();
                if (hook != null && Romby.IsValid(hook.ButtonMods) && Romby.IsValid(hook.ModsSection))
                {
                    UpdateTabBackgroundWidth(sm);
                    return;
                }

                if (hook == null)
                {
                    hook = sm.gameObject.AddComponent<ModManagerSettingsHook>();
                }
                hook.SettingsManager = sm;

                // 1. Inject Tab Button [Моды]
                InjectTabButton(sm, hook);

                // 2. Inject Mods Section Panel
                InjectModsSection(sm, hook);

                Debug.Log("[RombyLib.ModManager] Successfully injected native-styled [Моды] tab into SettingsManager.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] EnsureInjected failed: {ex}");
            }
        }

        private static void InjectTabButton(SettingsManager sm, ModManagerSettingsHook hook)
        {
            var othersBtn = sm.btnSectionOthers;
            var parent = othersBtn.transform.parent;

            var btnModsObj = Object.Instantiate(othersBtn.gameObject, parent);
            btnModsObj.name = "ButtonMods";
            hook.ButtonMods = btnModsObj;

            // Strip all LocalizedText and SmartLocalization components
            var locs = btnModsObj.GetComponentsInChildren<LocalizedText>(true);
            for (int i = 0; i < locs.Length; i++)
            {
                if (locs[i] != null)
                {
                    locs[i].StopAllCoroutines();
                    locs[i].enabled = false;
                    Object.DestroyImmediate(locs[i]);
                }
            }

            foreach (var mb in btnModsObj.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && mb.GetType().Name.IndexOf("Localiz", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    mb.enabled = false;
                    Object.DestroyImmediate(mb);
                }
            }

            // Match exact transform properties of othersBtn
            var othersRect = othersBtn.GetComponent<RectTransform>();
            var modsRect = btnModsObj.GetComponent<RectTransform>();
            modsRect.anchorMin = othersRect.anchorMin;
            modsRect.anchorMax = othersRect.anchorMax;
            modsRect.pivot = othersRect.pivot;
            modsRect.localScale = othersRect.localScale;
            modsRect.sizeDelta = othersRect.sizeDelta;

            // Spacing between tab buttons: in native game it's 189.11f
            float spacing = 189.11f;
            if (Romby.IsValid(sm.btnSectionLanguage))
            {
                var langRect = sm.btnSectionLanguage.GetComponent<RectTransform>();
                float delta = othersRect.anchoredPosition.x - langRect.anchoredPosition.x;
                if (delta > 250f) spacing = delta / 2f;
                else if (delta > 50f) spacing = delta;
            }

            modsRect.anchoredPosition = new Vector2(othersRect.anchoredPosition.x + spacing, othersRect.anchoredPosition.y);

            // Update tab label to "Mods"
            var labelTmp = btnModsObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (labelTmp != null)
            {
                labelTmp.text = "Mods";
                labelTmp.SetText("Mods");
                var f = NativeSettingsFont ?? UI.GameFont;
                if (f != null) labelTmp.font = f;
                hook.ButtonModsText = labelTmp;
            }

            // Hook click event
            var btn = btnModsObj.GetComponent<Button>();
            hook.ButtonImage = btnModsObj.GetComponent<Image>();
            if (hook.ButtonImage != null)
            {
                hook.ButtonImage.color = TabNormalColor;
            }

            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(new Action(() =>
            {
                ShowModsSection(sm);
            }));

            UpdateTabBackgroundWidth(sm);
        }

        /// <summary>
        /// Checks if the in-game Controls tab is currently present and active in the tab row.
        /// In main menu, ButtonOtros is placed at pos.x = 829.9f directly covering ButtonControles (pos.x = 826.1f).
        /// In game pause menu, ButtonOtros is shifted after Controls to pos.x = 1015.2f (> 950f).
        /// </summary>
        public static bool HasInGameControlsTab(SettingsManager sm)
        {
            if (!Romby.IsValid(sm) || !Romby.IsValid(sm.btnSectionOthers)) return false;
            var rt = sm.btnSectionOthers.GetComponent<RectTransform>();
            return rt != null && rt.anchoredPosition.x > 950f;
        }

        /// <summary>
        /// Computes the target width for sectionBackground (Fondo) and divider (Barra).
        /// Main menu (5 tabs) uses native 964px; In-game pause menu (6 tabs with Controls) uses 1153px.
        /// </summary>
        public static float GetTargetPanelWidth(SettingsManager sm)
        {
            return HasInGameControlsTab(sm) ? 1153.06f : 964.06f;
        }

        /// <summary>
        /// Dynamically adjusts the sectionBackground (Fondo) and header divider (Barra) width so all tabs fit cleanly.
        /// Also updates ButtonMods positioning and horizontal centering of all internal mod controls.
        /// </summary>
        public static void UpdateTabBackgroundWidth(SettingsManager sm)
        {
            if (!Romby.IsValid(sm)) return;

            float targetWidth = GetTargetPanelWidth(sm);

            if (Romby.IsValid(sm.sectionBackground))
            {
                sm.sectionBackground.sizeDelta = new Vector2(targetWidth, sm.sectionBackground.sizeDelta.y);
            }

            var barra = sm.transform.Find("Barra");
            if (barra != null)
            {
                var bRect = barra.GetComponent<RectTransform>();
                if (bRect != null)
                {
                    bRect.sizeDelta = new Vector2(targetWidth, bRect.sizeDelta.y);
                }
            }

            var hook = sm.GetComponent<ModManagerSettingsHook>();
            if (hook != null)
            {
                // Ensure ButtonMods is correctly positioned after ButtonOtros
                if (Romby.IsValid(hook.ButtonMods) && Romby.IsValid(sm.btnSectionOthers))
                {
                    var oRect = sm.btnSectionOthers.GetComponent<RectTransform>();
                    var mRect = hook.ButtonMods.GetComponent<RectTransform>();
                    if (oRect != null && mRect != null)
                    {
                        float spacing = 189.11f;
                        if (Romby.IsValid(sm.btnSectionLanguage))
                        {
                            var langRect = sm.btnSectionLanguage.GetComponent<RectTransform>();
                            if (langRect != null)
                            {
                                float delta = oRect.anchoredPosition.x - langRect.anchoredPosition.x;
                                if (delta > 250f) spacing = delta / 2f;
                                else if (delta > 50f) spacing = delta;
                            }
                        }
                        mRect.anchoredPosition = new Vector2(oRect.anchoredPosition.x + spacing, oRect.anchoredPosition.y);
                    }
                }

                // Update content horizontal centering
                UpdateContentCentering(sm, hook);
            }
        }

        /// <summary>
        /// Centers the internal mod settings panel content symmetrically within the Fondo background.
        /// In 964px panel (main menu), base center offsets apply.
        /// In 1153px panel (in-game pause menu), content is shifted right by +94.5px so margins are identical on both sides.
        /// </summary>
        public static void UpdateContentCentering(SettingsManager sm, ModManagerSettingsHook hook)
        {
            if (!Romby.IsValid(hook) || !Romby.IsValid(hook.ModsSection)) return;

            bool hasControls = HasInGameControlsTab(sm);
            float offset = hasControls ? 94.5f : 0f;

            var title = hook.ModsSection.transform.Find("Title_Mods");
            if (title != null)
            {
                var tr = title.GetComponent<RectTransform>();
                if (tr != null) tr.anchoredPosition = new Vector2(299.75f + offset, 32f);
            }

            var pwr = hook.ModsSection.transform.Find("PoweredBy_Mods");
            if (pwr != null)
            {
                var pr = pwr.GetComponent<RectTransform>();
                if (pr != null) pr.anchoredPosition = new Vector2(695f + offset, 32f);
            }

            var sel = hook.ModsSection.transform.Find("ModSelectorBar");
            if (sel != null)
            {
                var sr = sel.GetComponent<RectTransform>();
                if (sr != null) sr.anchoredPosition = new Vector2(299.75f + offset, -16f);
            }

            if (hook.ScrollViewRect != null)
            {
                hook.ScrollViewRect.anchoredPosition = new Vector2(299.75f + offset, -56f);
            }
        }



        private static void InjectModsSection(SettingsManager sm, ModManagerSettingsHook hook)
        {
            var parent = sm.othersSection != null ? sm.othersSection.transform.parent : sm.transform;

            // Root Section Container matching vanilla section coordinates
            var sectionObj = new GameObject("ModsSection");
            sectionObj.transform.SetParent(parent, false);
            hook.ModsSection = sectionObj;

            var rect = sectionObj.AddComponent<RectTransform>();
            if (Romby.IsValid(sm.othersSection))
            {
                var oRect = sm.othersSection.GetComponent<RectTransform>();
                rect.anchorMin = oRect.anchorMin;
                rect.anchorMax = oRect.anchorMax;
                rect.pivot = oRect.pivot;
                rect.anchoredPosition = oRect.anchoredPosition;
                rect.sizeDelta = oRect.sizeDelta;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(307.12f, -283.02f);
                rect.sizeDelta = new Vector2(100f, 100f);
            }

            var cg = sectionObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            hook.ModsCanvasGroup = cg;

            // Build Internal UI
            BuildModsSectionContent(sm, hook);

            sectionObj.SetActive(false);
        }

        private static void BuildModsSectionContent(SettingsManager sm, ModManagerSettingsHook hook)
        {
            var root = hook.ModsSection.transform;

            // 1. Title Header: Cloned from vanilla othersSection title for 100% visual authenticity
            GameObject titleObj;
            if (sm.othersSection != null && sm.othersSection.transform.childCount > 0)
            {
                titleObj = Object.Instantiate(sm.othersSection.transform.GetChild(0).gameObject, root);
                titleObj.name = "Title_Mods";
                var loc = titleObj.GetComponent<LocalizedText>();
                if (loc != null) { loc.enabled = false; Object.DestroyImmediate(loc); }
            }
            else
            {
                titleObj = new GameObject("Title_Mods");
                titleObj.transform.SetParent(root, false);
                var tRect = titleObj.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.5f, 0.5f);
                tRect.anchorMax = new Vector2(0.5f, 0.5f);
                tRect.pivot = new Vector2(0.5f, 0.5f);
                tRect.anchoredPosition = new Vector2(299.75f, 32f);
                tRect.sizeDelta = new Vector2(799.5f, 50f);
            }

            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            var tFont = NativeSettingsFont ?? UI.GameFont;
            if (titleTmp != null)
            {
                titleTmp.text = "Mods";
                titleTmp.SetText("Mods");
                if (tFont != null) titleTmp.font = tFont;
                titleTmp.fontSize = 42f;
                titleTmp.color = Color.white;
            }

            // 2. Powered by RombyLib Brand Header (top-right)
            var pwrObj = new GameObject("PoweredBy_Mods");
            pwrObj.transform.SetParent(root, false);
            var pwrRect = pwrObj.AddComponent<RectTransform>();
            pwrRect.anchorMin = new Vector2(0.5f, 0.5f);
            pwrRect.anchorMax = new Vector2(0.5f, 0.5f);
            pwrRect.pivot = new Vector2(1f, 0.5f);
            pwrRect.anchoredPosition = new Vector2(695f, 32f);
            pwrRect.sizeDelta = new Vector2(240f, 32f);

            var pwrTmp = pwrObj.AddComponent<TextMeshProUGUI>();
            pwrTmp.text = "<size=18><color=#73A8C0>Powered by</color> <color=#99E7FF>RombyLib</color></size>";
            pwrTmp.fontSize = 18f;
            pwrTmp.color = Color.white;
            pwrTmp.alignment = TextAlignmentOptions.MidlineRight;
            if (tFont != null) pwrTmp.font = tFont;

            // 3. Mod Selector Bar (Centered prominently below header)
            var selectorObj = new GameObject("ModSelectorBar");
            selectorObj.transform.SetParent(root, false);
            var selRect = selectorObj.AddComponent<RectTransform>();
            selRect.anchorMin = new Vector2(0.5f, 0.5f);
            selRect.anchorMax = new Vector2(0.5f, 0.5f);
            selRect.pivot = new Vector2(0.5f, 0.5f);
            selRect.anchoredPosition = new Vector2(299.75f, -16f);
            selRect.sizeDelta = new Vector2(460f, 32f);

            var selHlg = selectorObj.AddComponent<HorizontalLayoutGroup>();
            selHlg.childControlWidth = false;
            selHlg.childControlHeight = false;
            selHlg.childForceExpandWidth = false;
            selHlg.childAlignment = TextAnchor.MiddleCenter;
            selHlg.spacing = 10f;

            // Prev Mod Button "<"
            var prevModBtnObj = new GameObject("PrevMod");
            prevModBtnObj.transform.SetParent(selectorObj.transform, false);
            var pRect = prevModBtnObj.AddComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(36f, 28f);
            var pLe = prevModBtnObj.AddComponent<LayoutElement>();
            pLe.preferredWidth = 36f;
            pLe.preferredHeight = 28f;
            var pImg = prevModBtnObj.AddComponent<Image>();
            pImg.sprite = NativeButtonSprite ?? UISpriteFactory.Panel;
            pImg.type = Image.Type.Sliced;
            pImg.color = TabNormalColor;
            var pBtn = prevModBtnObj.AddComponent<Button>();
            pBtn.targetGraphic = pImg;
            var pColors = pBtn.colors;
            pColors.normalColor = TabNormalColor;
            pColors.highlightedColor = TabActiveColor;
            pBtn.colors = pColors;
            hook.PrevModButton = prevModBtnObj;

            var ptObj = new GameObject("T");
            ptObj.transform.SetParent(prevModBtnObj.transform, false);
            var ptRect = ptObj.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.sizeDelta = Vector2.zero;
            var ptTmp = ptObj.AddComponent<TextMeshProUGUI>();
            ptTmp.text = "<";
            ptTmp.fontSize = 20f;
            ptTmp.color = NativeCyanCheck;
            ptTmp.alignment = TextAlignmentOptions.Center;
            ptTmp.raycastTarget = false;
            if (tFont != null) ptTmp.font = tFont;

            // Mod Name Text (Centered)
            var nameObj = new GameObject("ModName");
            nameObj.transform.SetParent(selectorObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(340f, 30f);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 22f;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            if (tFont != null) nameTmp.font = tFont;
            var nLe = nameObj.AddComponent<LayoutElement>();
            nLe.preferredWidth = 340f;
            hook.ModSelectorText = nameTmp;

            // Next Mod Button ">"
            var nextModBtnObj = new GameObject("NextMod");
            nextModBtnObj.transform.SetParent(selectorObj.transform, false);
            var nRect = nextModBtnObj.AddComponent<RectTransform>();
            nRect.sizeDelta = new Vector2(36f, 28f);
            var nLe2 = nextModBtnObj.AddComponent<LayoutElement>();
            nLe2.preferredWidth = 36f;
            nLe2.preferredHeight = 28f;
            var nImg = nextModBtnObj.AddComponent<Image>();
            nImg.sprite = NativeButtonSprite ?? UISpriteFactory.Panel;
            nImg.type = Image.Type.Sliced;
            nImg.color = TabNormalColor;
            var nBtn = nextModBtnObj.AddComponent<Button>();
            nBtn.targetGraphic = nImg;
            var nColors = nBtn.colors;
            nColors.normalColor = TabNormalColor;
            nColors.highlightedColor = TabActiveColor;
            nBtn.colors = nColors;
            hook.NextModButton = nextModBtnObj;

            var ntObj = new GameObject("T");
            ntObj.transform.SetParent(nextModBtnObj.transform, false);
            var ntRect = ntObj.AddComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;
            ntRect.sizeDelta = Vector2.zero;
            var ntTmp = ntObj.AddComponent<TextMeshProUGUI>();
            ntTmp.text = ">";
            ntTmp.fontSize = 20f;
            ntTmp.color = NativeCyanCheck;
            ntTmp.alignment = TextAlignmentOptions.Center;
            ntTmp.raycastTarget = false;
            if (tFont != null) ntTmp.font = tFont;

            pBtn.onClick.AddListener(new Action(() => CycleMod(hook, -1)));
            nBtn.onClick.AddListener(new Action(() => CycleMod(hook, 1)));

            // 4. Settings Scroll View (Transparent, perfectly fits inside native teal background)
            var scrollObj = new GameObject("SettingsScrollView");
            scrollObj.transform.SetParent(root, false);
            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(299.75f, -56f);
            scrollRect.sizeDelta = new Vector2(800f, Mathf.Max(180f, hook.CurrentTargetHeight - 261f - 30f));
            hook.ScrollViewRect = scrollRect;

            var sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 35f;

            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var vpRect = viewportObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            var vpImg = viewportObj.AddComponent<Image>();
            vpImg.color = Color.clear;
            viewportObj.AddComponent<RectMask2D>();

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 0f);

            var cVlg = contentObj.AddComponent<VerticalLayoutGroup>();
            cVlg.spacing = 6f;
            cVlg.padding = new RectOffset(0, 10, 4, 15);
            cVlg.childControlWidth = true;
            cVlg.childControlHeight = true;
            cVlg.childForceExpandWidth = true;
            cVlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRect;
            sr.content = cRect;
            hook.SettingsContentContainer = cRect;
        }

        private static void CycleMod(ModManagerSettingsHook hook, int direction)
        {
            var mods = ModManager.AllMods;
            if (mods.Count == 0) return;

            int curIdx = 0;
            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i] == hook.SelectedMod) { curIdx = i; break; }
            }

            int nextIdx = (curIdx + direction + mods.Count) % mods.Count;
            hook.CurrentModIndex = nextIdx;
            hook.SelectedMod = mods[nextIdx];
            hook.UpdateBackgroundHeight(instant: false);
            RefreshModSettings(hook, hook.SelectedMod);
        }

        /// <summary>
        /// Shows the custom Mods section, hides vanilla sections, and populates the settings.
        /// </summary>
        public static void ShowModsSection(SettingsManager sm)
        {
            try
            {
                var hook = sm.GetComponent<ModManagerSettingsHook>();
                if (hook == null) return;

                // 1. Deactivate vanilla sections
                if (Romby.IsValid(sm.graphicsSection)) sm.graphicsSection.SetActive(false);
                if (Romby.IsValid(sm.audioSection)) sm.audioSection.SetActive(false);
                if (Romby.IsValid(sm.languageSection)) sm.languageSection.SetActive(false);
                if (Romby.IsValid(sm.othersSection)) sm.othersSection.SetActive(false);
                if (Romby.IsValid(sm.controlsSection)) sm.controlsSection.SetActive(false);

                // 2. Unhighlight vanilla tab buttons
                SetTabButtonColor(sm.btnSectionGraphics, TabNormalColor);
                SetTabButtonColor(sm.btnSectionAudio, TabNormalColor);
                SetTabButtonColor(sm.btnSectionLanguage, TabNormalColor);
                SetTabButtonColor(sm.btnSectionOthers, TabNormalColor);
                SetTabButtonColor(sm.btnSectionControls, TabNormalColor);

                // 3. Highlight [Моды] tab button
                if (hook.ButtonImage != null)
                {
                    hook.ButtonImage.color = TabActiveColor;
                }

                // 4. Update section background height dynamically to match mod content
                hook.UpdateBackgroundHeight(instant: false);

                // 5. Activate custom Mods section!
                if (Romby.IsValid(hook.ModsSection))
                {
                    hook.ModsSection.SetActive(true);
                    if (hook.ModsCanvasGroup != null)
                    {
                        hook.ModsCanvasGroup.alpha = 1f;
                        hook.ModsCanvasGroup.interactable = true;
                        hook.ModsCanvasGroup.blocksRaycasts = true;
                    }
                }

                // 6. Update SettingsManager current section reference
                sm._currentSection = hook.ModsSection;

                // 7. Select default mod and refresh rows
                var mods = ModManager.AllMods;
                if (mods.Count > 0)
                {
                    bool exists = false;
                    for (int i = 0; i < mods.Count; i++)
                    {
                        if (mods[i] == hook.SelectedMod) { exists = true; break; }
                    }
                    if (hook.SelectedMod == null || !exists)
                    {
                        hook.CurrentModIndex = 0;
                        hook.SelectedMod = mods[0];
                    }
                }

                RefreshModSettings(hook, hook.SelectedMod);
                hook.UpdateBackgroundHeight(instant: false);

                // 8. Dump hierarchy to logs for inspection
                Test.DumpHierarchy(sm.gameObject, maxDepth: 4);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.ModManager] ShowModsSection error: {ex}");
            }
        }

        private static void SetTabButtonColor(Button btn, Color color)
        {
            if (Romby.IsValid(btn))
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = color;
            }
        }

        /// <summary>
        /// Callback when a vanilla section is shown, resetting the [Моды] tab highlight and hiding our panel.
        /// </summary>
        public static void OnVanillaSectionShown(SettingsManager sm, GameObject section)
        {
            try
            {
                var hook = sm.GetComponent<ModManagerSettingsHook>();
                if (hook == null) return;

                if (hook.ModsSection != null && section != hook.ModsSection)
                {
                    hook.ModsSection.SetActive(false);
                    if (hook.ModsCanvasGroup != null)
                    {
                        hook.ModsCanvasGroup.alpha = 0f;
                        hook.ModsCanvasGroup.interactable = false;
                        hook.ModsCanvasGroup.blocksRaycasts = false;
                    }

                    if (hook.ButtonImage != null)
                    {
                        hook.ButtonImage.color = TabNormalColor;
                    }
                }
            }
            catch { }
        }

        private static void RefreshModSettings(ModManagerSettingsHook hook, ModDefinition mod)
        {
            if (mod == null) return;

            if (hook.ModSelectorText != null)
            {
                var mods = ModManager.AllMods;
                int currentNumber = 1;
                for (int i = 0; i < mods.Count; i++)
                {
                    if (mods[i] == mod) { currentNumber = i + 1; break; }
                }

                bool canSwitch = mods.Count > 1;

                if (hook.PrevModButton != null)
                {
                    hook.PrevModButton.SetActive(true);
                    var pBtn = hook.PrevModButton.GetComponent<Button>();
                    if (pBtn != null) pBtn.interactable = canSwitch;
                    var pImg = hook.PrevModButton.GetComponent<Image>();
                    if (pImg != null) pImg.color = canSwitch ? TabNormalColor : new Color(TabNormalColor.r, TabNormalColor.g, TabNormalColor.b, 0.4f);
                }

                if (hook.NextModButton != null)
                {
                    hook.NextModButton.SetActive(true);
                    var nBtn = hook.NextModButton.GetComponent<Button>();
                    if (nBtn != null) nBtn.interactable = canSwitch;
                    var nImg = hook.NextModButton.GetComponent<Image>();
                    if (nImg != null) nImg.color = canSwitch ? TabNormalColor : new Color(TabNormalColor.r, TabNormalColor.g, TabNormalColor.b, 0.4f);
                }

                if (canSwitch)
                {
                    hook.ModSelectorText.text = $"{mod.Name} <color=#{ColorUtility.ToHtmlStringRGB(NativeCyanCheck)}>v{mod.Version} ({currentNumber}/{mods.Count})</color>";
                }
                else
                {
                    hook.ModSelectorText.text = $"{mod.Name} <color=#{ColorUtility.ToHtmlStringRGB(NativeCyanCheck)}>v{mod.Version}</color>";
                }
            }

            var container = hook.SettingsContentContainer;
            if (container == null) return;

            // Clear old rows
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(container.GetChild(i).gameObject);
            }

            var categories = new Dictionary<string, (List<ModSettingEntry> settings, List<ModActionEntry> actions)>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in mod.Settings)
            {
                string cat = !string.IsNullOrEmpty(s.Category) ? s.Category : "General";
                if (!categories.ContainsKey(cat)) categories[cat] = (new List<ModSettingEntry>(), new List<ModActionEntry>());
                categories[cat].settings.Add(s);
            }

            foreach (var a in mod.Actions)
            {
                string cat = !string.IsNullOrEmpty(a.Category) ? a.Category : "Actions";
                if (!categories.ContainsKey(cat)) categories[cat] = (new List<ModSettingEntry>(), new List<ModActionEntry>());
                categories[cat].actions.Add(a);
            }

            foreach (var kvp in categories)
            {
                // Clean Category Header (Plain text, no missing glyphs)
                BuildCategoryHeader(container, kvp.Key);

                // Settings Rows
                foreach (var s in kvp.Value.settings)
                {
                    BuildSettingRow(container, s);
                }

                // Action Buttons
                foreach (var a in kvp.Value.actions)
                {
                    BuildActionButtonRow(container, a);
                }
            }

            // Bottom Action Bar: Reset & Auto-Save info
            BuildBottomBar(container, hook, mod);
        }

        private static void BuildCategoryHeader(Transform parent, string categoryName)
        {
            var catObj = new GameObject("Cat_" + categoryName);
            catObj.transform.SetParent(parent, false);

            var le = catObj.AddComponent<LayoutElement>();
            le.minHeight = 32f;
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;

            var tmp = catObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(NativeCyanCheck)}>--</color> {categoryName.ToUpper()} <color=#{ColorUtility.ToHtmlStringRGB(NativeCyanCheck)}>--</color>";
            tmp.fontSize = 22f;
            tmp.color = NativeCyanCheck;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var catFont = NativeSettingsFont ?? UI.GameFont;
            if (catFont != null) tmp.font = catFont;
        }

        private static void BuildSettingRow(Transform parent, ModSettingEntry setting)
        {
            var rowObj = new GameObject("Row_" + setting.Key);
            rowObj.transform.SetParent(parent, false);

            var le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;
            le.flexibleWidth = 1f;

            var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 20, 0, 0);
            hlg.spacing = 20f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // Setting Label on Left
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(rowObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(440f, 40f);

            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = setting.DisplayName;
            nameTmp.fontSize = 28f;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            var rowFont = NativeSettingsFont ?? UI.GameFont;
            if (rowFont != null) nameTmp.font = rowFont;

            var nameLe = nameObj.AddComponent<LayoutElement>();
            nameLe.minWidth = 440f;
            nameLe.preferredWidth = 440f;
            nameLe.flexibleWidth = 0f;

            // Control Slot on Right (280px wide, controls start at the left of this slot)
            var slotObj = new GameObject("Slot");
            slotObj.transform.SetParent(rowObj.transform, false);
            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(280f, 40f);

            var slotLe = slotObj.AddComponent<LayoutElement>();
            slotLe.minWidth = 280f;
            slotLe.preferredWidth = 280f;
            slotLe.flexibleWidth = 0f;

            var type = setting.ValueType;
            if (type == typeof(bool))
            {
                BuildNativeCheckbox(slotObj.transform, setting);
            }
            else if (type == typeof(float) || type == typeof(double) || type == typeof(int))
            {
                BuildNativeSlider(slotObj.transform, setting);
            }
            else if (type.IsEnum)
            {
                BuildNativeEnumStepper(slotObj.transform, setting);
            }
            else
            {
                var valObj = new GameObject("Val");
                valObj.transform.SetParent(slotObj.transform, false);
                var vRect = valObj.AddComponent<RectTransform>();
                vRect.anchorMin = new Vector2(0f, 0.5f);
                vRect.anchorMax = new Vector2(0f, 0.5f);
                vRect.pivot = new Vector2(0f, 0.5f);
                vRect.anchoredPosition = Vector2.zero;
                vRect.sizeDelta = new Vector2(260f, 34f);

                var vTmp = valObj.AddComponent<TextMeshProUGUI>();
                vTmp.text = setting.GetValue()?.ToString() ?? "";
                vTmp.fontSize = 22f;
                vTmp.color = NativeCyanCheck;
                vTmp.alignment = TextAlignmentOptions.MidlineLeft;
                if (UI.GameFont != null) vTmp.font = UI.GameFont;
            }
        }

        private static void BuildNativeCheckbox(Transform parent, ModSettingEntry setting)
        {
            var boxObj = new GameObject("ToggleBox");
            boxObj.transform.SetParent(parent, false);

            var bRect = boxObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0.5f);
            bRect.anchorMax = new Vector2(0f, 0.5f);
            bRect.pivot = new Vector2(0f, 0.5f);
            bRect.anchoredPosition = Vector2.zero;
            bRect.sizeDelta = new Vector2(28f, 28f);

            var img = boxObj.AddComponent<Image>();
            img.sprite = NativeToggleSprite ?? UISpriteFactory.Panel;
            img.type = Image.Type.Sliced;
            img.color = TabNormalColor;

            var btn = boxObj.AddComponent<Button>();
            btn.targetGraphic = img;
            var cbColors = btn.colors;
            cbColors.normalColor = TabNormalColor;
            cbColors.highlightedColor = new Color(0f, 0.48f, 0.62f, 1f);
            cbColors.pressedColor = new Color(0f, 0.28f, 0.38f, 1f);
            cbColors.selectedColor = TabNormalColor;
            btn.colors = cbColors;

            // Checkmark Cross [✕] using native Image Icon_FlatWhite_Delete or fallback Latin 'X'
            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(boxObj.transform, false);
            var cRect = checkObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(20f, 20f);

            if (NativeCheckmarkSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in allSprites)
                {
                    if (s != null && s.name.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        NativeCheckmarkSprite = s;
                        break;
                    }
                }
            }

            if (NativeCheckmarkSprite != null)
            {
                var cImg = checkObj.AddComponent<Image>();
                cImg.sprite = NativeCheckmarkSprite;
                cImg.color = NativeCyanCheck;
                cImg.raycastTarget = false;
            }
            else
            {
                var cTmp = checkObj.AddComponent<TextMeshProUGUI>();
                cTmp.text = "<b>X</b>";
                cTmp.fontSize = 17f;
                cTmp.color = NativeCyanCheck;
                cTmp.alignment = TextAlignmentOptions.Center;
                cTmp.raycastTarget = false;
                if (UI.GameFont != null) cTmp.font = UI.GameFont;
            }

            bool currentVal = (bool)setting.GetValue();
            checkObj.SetActive(currentVal);

            btn.onClick.AddListener(new Action(() =>
            {
                bool newVal = !(bool)setting.GetValue();
                setting.SetValue(newVal);
                checkObj.SetActive(newVal);
            }));
        }

        private static void BuildNativeSlider(Transform parent, ModSettingEntry setting)
        {
            var sliderObj = new GameObject("Slider_" + setting.Key);
            sliderObj.transform.SetParent(parent, false);

            var sRect = sliderObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0.5f);
            sRect.anchorMax = new Vector2(0f, 0.5f);
            sRect.pivot = new Vector2(0f, 0.5f);
            sRect.anchoredPosition = Vector2.zero;
            sRect.sizeDelta = new Vector2(260f, 26f);

            // Background Track (Rounded pill)
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = NativeSliderSprite ?? UISpriteFactory.Panel;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = SliderTrackColor;

            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = NativeSliderSprite ?? UISpriteFactory.Panel;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = SliderFillColor;

            // Centered Value Text inside the Slider Track
            var valObj = new GameObject("ValueText");
            valObj.transform.SetParent(sliderObj.transform, false);
            var vRect = valObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            var valTmp = valObj.AddComponent<TextMeshProUGUI>();
            valTmp.fontSize = 20f;
            valTmp.color = Color.white;
            valTmp.alignment = TextAlignmentOptions.Center;
            valTmp.raycastTarget = false;
            var slFont = NativeSettingsFont ?? UI.GameFont;
            if (slFont != null) valTmp.font = slFont;

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = bgImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = setting.Attribute.Min;
            slider.maxValue = setting.Attribute.Max;

            float curFloat = Convert.ToSingle(setting.GetValue(), CultureInfo.InvariantCulture);
            slider.value = curFloat;

            void UpdateText(float val)
            {
                if (setting.ValueType == typeof(int))
                {
                    valTmp.text = string.Format(setting.Attribute.Format, (int)Math.Round(val));
                }
                else
                {
                    valTmp.text = string.Format(setting.Attribute.Format, val);
                }
            }
            UpdateText(curFloat);

            slider.onValueChanged.AddListener(new Action<float>(val =>
            {
                if (setting.ValueType == typeof(int))
                {
                    int intVal = (int)Math.Round(val);
                    setting.SetValue(intVal);
                    UpdateText(intVal);
                }
                else
                {
                    setting.SetValue(val);
                    UpdateText(val);
                }
            }));
        }

        private static void BuildNativeEnumStepper(Transform parent, ModSettingEntry setting)
        {
            var ctrlObj = new GameObject("EnumStepper");
            ctrlObj.transform.SetParent(parent, false);

            var sRect = ctrlObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0.5f);
            sRect.anchorMax = new Vector2(0f, 0.5f);
            sRect.pivot = new Vector2(0f, 0.5f);
            sRect.anchoredPosition = Vector2.zero;
            sRect.sizeDelta = new Vector2(260f, 28f);

            var hlg = ctrlObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var values = Enum.GetValues(setting.ValueType);

            // Left Arrow
            var prevObj = new GameObject("P");
            prevObj.transform.SetParent(ctrlObj.transform, false);
            var pRect = prevObj.AddComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(28f, 28f);
            var pLe = prevObj.AddComponent<LayoutElement>();
            pLe.preferredWidth = 28f;
            pLe.preferredHeight = 28f;
            var pImg = prevObj.AddComponent<Image>();
            pImg.sprite = NativeButtonSprite ?? UISpriteFactory.Panel;
            pImg.type = Image.Type.Sliced;
            pImg.color = TabNormalColor;
            var pBtn = prevObj.AddComponent<Button>();
            pBtn.targetGraphic = pImg;

            var ptObj = new GameObject("T");
            ptObj.transform.SetParent(prevObj.transform, false);
            var ptRect = ptObj.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.sizeDelta = Vector2.zero;
            var ptTmp = ptObj.AddComponent<TextMeshProUGUI>();
            ptTmp.text = "<b><</b>";
            ptTmp.fontSize = 18f;
            ptTmp.color = NativeCyanCheck;
            ptTmp.alignment = TextAlignmentOptions.Center;

            // Center Label
            var centerObj = new GameObject("V");
            centerObj.transform.SetParent(ctrlObj.transform, false);
            var cTmp = centerObj.AddComponent<TextMeshProUGUI>();
            cTmp.fontSize = 22f;
            cTmp.color = Color.white;
            cTmp.alignment = TextAlignmentOptions.Center;
            var enFont = NativeSettingsFont ?? UI.GameFont;
            if (enFont != null) cTmp.font = enFont;
            var cLe = centerObj.AddComponent<LayoutElement>();
            cLe.preferredWidth = 190f;

            void UpdateLabel()
            {
                cTmp.text = $"<b>{setting.GetValue()}</b>";
            }
            UpdateLabel();

            // Right Arrow
            var nextObj = new GameObject("N");
            nextObj.transform.SetParent(ctrlObj.transform, false);
            var nRect = nextObj.AddComponent<RectTransform>();
            nRect.sizeDelta = new Vector2(28f, 28f);
            var nLe = nextObj.AddComponent<LayoutElement>();
            nLe.preferredWidth = 28f;
            nLe.preferredHeight = 28f;
            var nImg = nextObj.AddComponent<Image>();
            nImg.sprite = NativeButtonSprite ?? UISpriteFactory.Panel;
            nImg.type = Image.Type.Sliced;
            nImg.color = TabNormalColor;
            var nBtn = nextObj.AddComponent<Button>();
            nBtn.targetGraphic = nImg;

            var ntObj = new GameObject("T");
            ntObj.transform.SetParent(nextObj.transform, false);
            var ntRect = ntObj.AddComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;
            ntRect.sizeDelta = Vector2.zero;
            var ntTmp = ntObj.AddComponent<TextMeshProUGUI>();
            ntTmp.text = "<b>></b>";
            ntTmp.fontSize = 18f;
            ntTmp.color = NativeCyanCheck;
            ntTmp.alignment = TextAlignmentOptions.Center;

            int GetCurrentIndex()
            {
                object cur = setting.GetValue();
                for (int i = 0; i < values.Length; i++)
                {
                    if (values.GetValue(i).Equals(cur)) return i;
                }
                return 0;
            }

            pBtn.onClick.AddListener(new Action(() =>
            {
                int idx = GetCurrentIndex() - 1;
                if (idx < 0) idx = values.Length - 1;
                setting.SetValue(values.GetValue(idx));
                UpdateLabel();
            }));

            nBtn.onClick.AddListener(new Action(() =>
            {
                int idx = GetCurrentIndex() + 1;
                if (idx >= values.Length) idx = 0;
                setting.SetValue(values.GetValue(idx));
                UpdateLabel();
            }));
        }

        private static void BuildActionButtonRow(Transform parent, ModActionEntry action)
        {
            var rowObj = new GameObject("ActionRow_" + action.Name);
            rowObj.transform.SetParent(parent, false);

            var le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 46f;
            le.preferredHeight = 46f;
            le.flexibleWidth = 1f;

            var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 20, 0, 0);
            hlg.spacing = 20f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(rowObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(440f, 40f);

            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = action.Name;
            nameTmp.fontSize = 28f;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            var aFont = NativeSettingsFont ?? UI.GameFont;
            if (aFont != null) nameTmp.font = aFont;

            var nameLe = nameObj.AddComponent<LayoutElement>();
            nameLe.minWidth = 440f;
            nameLe.preferredWidth = 440f;
            nameLe.flexibleWidth = 0f;

            // Button Slot
            var slotObj = new GameObject("Slot");
            slotObj.transform.SetParent(rowObj.transform, false);
            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(280f, 40f);

            var slotLe = slotObj.AddComponent<LayoutElement>();
            slotLe.minWidth = 280f;
            slotLe.preferredWidth = 280f;
            slotLe.flexibleWidth = 0f;

            var btnObj = new GameObject("Btn");
            btnObj.transform.SetParent(slotObj.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0.5f);
            bRect.anchorMax = new Vector2(0f, 0.5f);
            bRect.pivot = new Vector2(0f, 0.5f);
            bRect.anchoredPosition = Vector2.zero;
            bRect.sizeDelta = new Vector2(240f, 34f);

            var bImg = btnObj.AddComponent<Image>();
            bImg.sprite = NativeButtonSprite ?? UISpriteFactory.Panel;
            bImg.type = Image.Type.Sliced;
            bImg.color = TabNormalColor;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bImg;
            var colors = btn.colors;
            colors.normalColor = TabNormalColor;
            colors.highlightedColor = new Color(0f, 0.48f, 0.62f, 1f);
            btn.colors = colors;

            var txtObj = new GameObject("T");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var tTmp = txtObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "Execute";
            tTmp.fontSize = 19f;
            tTmp.color = Color.white;
            tTmp.alignment = TextAlignmentOptions.Center;
            if (aFont != null) tTmp.font = aFont;

            btn.onClick.AddListener(new Action(() =>
            {
                action.Invoke();
            }));
        }

        private static void BuildBottomBar(Transform parent, ModManagerSettingsHook hook, ModDefinition mod)
        {
            var botObj = new GameObject("BottomBar");
            botObj.transform.SetParent(parent, false);

            var le = botObj.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;
            le.flexibleWidth = 1f;

            var hlg = botObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 20, 8, 4);
            hlg.spacing = 20f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // Auto-Save Notice on Left
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(botObj.transform, false);
            var iRect = infoObj.AddComponent<RectTransform>();
            iRect.sizeDelta = new Vector2(440f, 40f);

            var iTmp = infoObj.AddComponent<TextMeshProUGUI>();
            iTmp.text = "Settings are saved automatically";
            iTmp.fontSize = 20f;
            iTmp.color = NativeCyanCheck;
            iTmp.alignment = TextAlignmentOptions.MidlineLeft;
            var bFont = NativeSettingsFont ?? UI.GameFont;
            if (bFont != null) iTmp.font = bFont;

            var iLe = infoObj.AddComponent<LayoutElement>();
            iLe.minWidth = 440f;
            iLe.preferredWidth = 440f;
            iLe.flexibleWidth = 0f;

            // Reset Button on Right
            var slotObj = new GameObject("Slot");
            slotObj.transform.SetParent(botObj.transform, false);
            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(280f, 40f);

            var slotLe = slotObj.AddComponent<LayoutElement>();
            slotLe.minWidth = 280f;
            slotLe.preferredWidth = 280f;
            slotLe.flexibleWidth = 0f;

            var rBtnObj = new GameObject("ResetBtn");
            rBtnObj.transform.SetParent(slotObj.transform, false);
            var rRect = rBtnObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 0.5f);
            rRect.anchorMax = new Vector2(0f, 0.5f);
            rRect.pivot = new Vector2(0f, 0.5f);
            rRect.anchoredPosition = Vector2.zero;
            rRect.sizeDelta = new Vector2(240f, 34f);

            var rImg = rBtnObj.AddComponent<Image>();
            rImg.sprite = NativeButtonSprite ?? UISpriteFactory.Panel;
            rImg.type = Image.Type.Sliced;
            rImg.color = TabNormalColor;

            var rBtn = rBtnObj.AddComponent<Button>();
            rBtn.targetGraphic = rImg;
            var rColors = rBtn.colors;
            rColors.normalColor = TabNormalColor;
            rColors.highlightedColor = UIColors.Red;
            rBtn.colors = rColors;

            var rtObj = new GameObject("T");
            rtObj.transform.SetParent(rBtnObj.transform, false);
            var rtRect = rtObj.AddComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.sizeDelta = Vector2.zero;
            var rtTmp = rtObj.AddComponent<TextMeshProUGUI>();
            rtTmp.text = "Reset to Default";
            rtTmp.fontSize = 18f;
            rtTmp.color = Color.white;
            rtTmp.alignment = TextAlignmentOptions.Center;
            if (bFont != null) rtTmp.font = bFont;

            rBtn.onClick.AddListener(new Action(() =>
            {
                mod.ResetAll();
                RefreshModSettings(hook, mod);
                hook.UpdateBackgroundHeight(instant: false);
                UI.ShowNotification($"Settings for '{mod.Name}' restored to defaults!", 3f, null, NotificationPosition.Bottom, NotificationSound.None);
            }));
        }
    }

    #endregion
}
