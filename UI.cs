using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Preset anchor positions for UI windows and elements.
    /// Supports exact coordinates, screen centering, edges, and corners.
    /// </summary>
    public enum UIAnchor
    {
        /// <summary>Center of the screen (0.5, 0.5).</summary>
        Center,
        /// <summary>Top-left screen corner (0, 1).</summary>
        TopLeft,
        /// <summary>Top-center screen edge (0.5, 1).</summary>
        TopCenter,
        /// <summary>Top-right screen corner (1, 1).</summary>
        TopRight,
        /// <summary>Middle-left screen edge (0, 0.5).</summary>
        MiddleLeft,
        /// <summary>Middle-right screen edge (1, 0.5).</summary>
        MiddleRight,
        /// <summary>Bottom-left screen corner (0, 0).</summary>
        BottomLeft,
        /// <summary>Bottom-center screen edge (0.5, 0).</summary>
        BottomCenter,
        /// <summary>Bottom-right screen corner (1, 0).</summary>
        BottomRight,
        /// <summary>Stretches across parent (0, 0) to (1, 1).</summary>
        Stretch
    }

    /// <summary>
    /// Screen location for in-game announcement and floating notifications.
    /// </summary>
    public enum NotificationPosition
    {
        /// <summary>Bottom-center of the screen, matching Rombykon's native NPC, unlock, and item dialogues.</summary>
        Bottom,
        /// <summary>Upper-center of the screen below top HUD, matching day/night and wave transition announcements.</summary>
        Top,
        /// <summary>Dead center of the screen, for boss defeat and epic milestone banners.</summary>
        Center
    }

    /// <summary>
    /// Sound effects available for in-game notifications and announcements.
    /// Modders can select from curated game sound presets or specify a custom clip name.
    /// </summary>
    public enum NotificationSound
    {
        /// <summary>No sound played.</summary>
        None = 0,
        /// <summary>Default notification sound (clean UI popup / chime from native game assets).</summary>
        Default = 1,
        /// <summary>Coin or money pickup sound.</summary>
        Coin = 2,
        /// <summary>Achievement, victory, fanfare, or level up sound.</summary>
        Success = 3,
        /// <summary>Tactile UI button click sound.</summary>
        Click = 4,
        /// <summary>Warning, error, hurt, or damage alert sound.</summary>
        Warning = 5,
        /// <summary>Crystal, spell, or magic card sound.</summary>
        Magic = 6
    }

    /// <summary>
    /// Signature cyberpunk and arcade color palette matching Rombykon's native UI aesthetics.
    /// </summary>
    public static class UIColors
    {
        /// <summary>Signature Rombykon cyan neon accent (#50E0FF).</summary>
        public static readonly Color Cyan = new Color(0.314f, 0.878f, 1f, 1f);

        /// <summary>Radiant gold/yellow accent (#FFD24D).</summary>
        public static readonly Color Gold = new Color(1f, 0.824f, 0.302f, 1f);

        /// <summary>Lilac/magenta neon accent (#C084FC).</summary>
        public static readonly Color Magenta = new Color(0.753f, 0.518f, 0.988f, 1f);

        /// <summary>Vibrant success green (#4ADE80).</summary>
        public static readonly Color Green = new Color(0.290f, 0.871f, 0.502f, 1f);

        /// <summary>Vibrant danger / close red (#F87171).</summary>
        public static readonly Color Red = new Color(0.973f, 0.443f, 0.443f, 1f);

        /// <summary>Crisp white text color (#F0F6FC).</summary>
        public static readonly Color TextWhite = new Color(0.941f, 0.965f, 0.988f, 1f);

        /// <summary>Subtle muted label grey (#8B949E).</summary>
        public static readonly Color TextMuted = new Color(0.545f, 0.580f, 0.620f, 1f);

        /// <summary>Deep obsidian indigo window background (#0B0F1DF6).</summary>
        public static readonly Color PanelBackground = new Color(0.035f, 0.047f, 0.094f, 0.965f);

        /// <summary>Lighter header bar navy (#0F162B).</summary>
        public static readonly Color HeaderBackground = new Color(0.055f, 0.075f, 0.145f, 0.98f);

        /// <summary>Subtle cyan border outline (#50E0FFA6).</summary>
        public static readonly Color BorderCyan = new Color(0.314f, 0.878f, 1f, 0.65f);

        /// <summary>Dark navy button normal state (#141A30).</summary>
        public static readonly Color ButtonNormal = new Color(0.078f, 0.102f, 0.188f, 1f);

        /// <summary>Glowing cyan-blue button hover state (#1E3A5F).</summary>
        public static readonly Color ButtonHover = new Color(0.118f, 0.227f, 0.373f, 1f);

        /// <summary>Deep midnight button pressed state (#112338).</summary>
        public static readonly Color ButtonPressed = new Color(0.067f, 0.137f, 0.220f, 1f);

        /// <summary>Dark track groove for sliders (#0A0D18).</summary>
        public static readonly Color SliderBackground = new Color(0.039f, 0.051f, 0.094f, 1f);

        /// <summary>Sleek hairline divider (#1E294B).</summary>
        public static readonly Color Divider = new Color(0.118f, 0.161f, 0.294f, 0.8f);
    }

    /// <summary>
    /// Factory that procedurally generates crisp, anti-aliased 9-sliced rounded rectangle sprites.
    /// Completely self-contained, independent of external assets, and pixel-perfect at any resolution.
    /// </summary>
    internal static class UISpriteFactory
    {
        private static Sprite _panelSprite;
        private static Sprite _headerSprite;
        private static Sprite _buttonNormalSprite;
        private static Sprite _buttonHoverSprite;
        private static Sprite _buttonPressedSprite;
        private static Sprite _sliderTrackSprite;
        private static Sprite _sliderFillSprite;
        private static Sprite _sliderKnobSprite;
        private static Sprite _toggleBgSprite;
        private static Sprite _whiteSprite;

        public static Sprite White => _whiteSprite ??= CreateSolidSprite(Color.white);

        public static Sprite Panel => _panelSprite ??= CreateRoundedRectSprite(64, 14f, 1.5f, UIColors.PanelBackground, UIColors.BorderCyan);

        public static Sprite Header => _headerSprite ??= CreateRoundedRectSprite(64, 14f, 0f, UIColors.HeaderBackground, Color.clear);

        public static Sprite ButtonNormal => _buttonNormalSprite ??= CreateRoundedRectSprite(48, 10f, 1.2f, UIColors.ButtonNormal, new Color(0.314f, 0.878f, 1f, 0.3f));

        public static Sprite ButtonHover => _buttonHoverSprite ??= CreateRoundedRectSprite(48, 10f, 1.8f, UIColors.ButtonHover, UIColors.Cyan);

        public static Sprite ButtonPressed => _buttonPressedSprite ??= CreateRoundedRectSprite(48, 10f, 1.5f, UIColors.ButtonPressed, UIColors.Cyan);

        public static Sprite SliderTrack => _sliderTrackSprite ??= CreateRoundedRectSprite(32, 6f, 1f, UIColors.SliderBackground, new Color(0.2f, 0.28f, 0.45f, 0.5f));

        public static Sprite SliderFill => _sliderFillSprite ??= CreateRoundedRectSprite(32, 6f, 0f, UIColors.Cyan, Color.clear);

        public static Sprite SliderKnob => _sliderKnobSprite ??= CreateCircleSprite(32, UIColors.Cyan, Color.white, 2f);

        public static Sprite ToggleBackground => _toggleBgSprite ??= CreateRoundedRectSprite(32, 6f, 1.2f, new Color(0.06f, 0.08f, 0.15f, 1f), new Color(0.314f, 0.878f, 1f, 0.4f));

        private static Sprite CreateSolidSprite(Color color)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var cols = new Color[] { color, color, color, color };
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateRoundedRectSprite(int size, float radius, float borderWidth, Color fillColor, Color borderColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float half = size * 0.5f;
            float w = half - 1f;
            float h = half - 1f;
            float r = Mathf.Min(radius, half - 1f);
            var colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - half + 0.5f;
                    float py = y - half + 0.5f;

                    float dx = Mathf.Max(Mathf.Abs(px) - (w - r), 0f);
                    float dy = Mathf.Max(Mathf.Abs(py) - (h - r), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - r;

                    float outerAlpha = Mathf.Clamp01(0.5f - dist);

                    if (dist < -borderWidth)
                    {
                        colors[y * size + x] = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * outerAlpha);
                    }
                    else if (outerAlpha > 0f)
                    {
                        float t = Mathf.Clamp01(dist + borderWidth);
                        Color blended = Color.Lerp(fillColor, borderColor, t);
                        colors[y * size + x] = new Color(blended.r, blended.g, blended.b, blended.a * outerAlpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            int border = Mathf.CeilToInt(radius) + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private static Sprite CreateCircleSprite(int size, Color fillColor, Color borderColor, float borderWidth)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float half = size * 0.5f;
            float radius = half - 1f;
            var colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - half + 0.5f;
                    float py = y - half + 0.5f;
                    float dist = Mathf.Sqrt(px * px + py * py) - radius;

                    float outerAlpha = Mathf.Clamp01(0.5f - dist);

                    if (dist < -borderWidth)
                    {
                        colors[y * size + x] = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * outerAlpha);
                    }
                    else if (outerAlpha > 0f)
                    {
                        float t = Mathf.Clamp01(dist + borderWidth);
                        Color blended = Color.Lerp(fillColor, borderColor, t);
                        colors[y * size + x] = new Color(blended.r, blended.g, blended.b, blended.a * outerAlpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>
    /// Smooth window drag controller for panel header bars.
    /// Registered into the IL2CPP runtime.
    /// </summary>
    public class UIDragHandler : MonoBehaviour
    {
        public UIDragHandler(IntPtr ptr) : base(ptr) { }

        public RectTransform TargetRect;
        public RectTransform CanvasRect;
        public RectTransform HeaderRect;

        private bool _isDragging;
        private Vector2 _dragOffset;

        private void Update()
        {
            if (TargetRect == null || CanvasRect == null || HeaderRect == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(HeaderRect, mousePos, null))
                {
                    _isDragging = true;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasRect, mousePos, null, out var localPoint);
                    _dragOffset = TargetRect.anchoredPosition - localPoint;
                }
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (_isDragging && mouse.leftButton.isPressed)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasRect, mousePos, null, out var localPoint);
                TargetRect.anchoredPosition = localPoint + _dragOffset;
            }
        }
    }

    /// <summary>
    /// Smooth floating animator for native-style in-game notifications.
    /// Registered into the IL2CPP runtime.
    /// </summary>
    public class UINotificationAnimator : MonoBehaviour
    {
        public UINotificationAnimator(IntPtr ptr) : base(ptr) { }

        public CanvasGroup CanvasGroup;
        public RectTransform RectTransform;
        public float Duration = 3.5f;
        public Vector2 StartPosition;
        public Vector2 TargetPosition;
        public Vector2 EndPosition;

        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            float fadeIn = 0.32f;
            float fadeOut = 0.45f;
            float total = Duration;

            if (_elapsed < fadeIn)
            {
                float t = Mathf.Clamp01(_elapsed / fadeIn);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                if (CanvasGroup != null) CanvasGroup.alpha = ease;
                if (RectTransform != null) RectTransform.anchoredPosition = Vector2.Lerp(StartPosition, TargetPosition, ease);
            }
            else if (_elapsed > total - fadeOut)
            {
                float t = Mathf.Clamp01((_elapsed - (total - fadeOut)) / fadeOut);
                float ease = t * t;
                if (CanvasGroup != null) CanvasGroup.alpha = 1f - ease;
                if (RectTransform != null) RectTransform.anchoredPosition = Vector2.Lerp(TargetPosition, EndPosition, ease);
            }
            else
            {
                if (CanvasGroup != null) CanvasGroup.alpha = 1f;
                if (RectTransform != null) RectTransform.anchoredPosition = TargetPosition;
            }

            if (_elapsed >= total)
            {
                Object.Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Wrapper for text labels inside UI panels.
    /// </summary>
    public class UILabel
    {
        public GameObject GameObject { get; }
        public TextMeshProUGUI TMP { get; }

        public UILabel(GameObject go, TextMeshProUGUI tmp)
        {
            GameObject = go;
            TMP = tmp;
        }

        /// <summary>Gets or sets the displayed text.</summary>
        public string Text
        {
            get => TMP != null ? TMP.text : string.Empty;
            set { if (TMP != null) TMP.text = value; }
        }

        /// <summary>Gets or sets the text color.</summary>
        public Color Color
        {
            get => TMP != null ? TMP.color : Color.white;
            set { if (TMP != null) TMP.color = value; }
        }

        /// <summary>Gets or sets the font size.</summary>
        public float FontSize
        {
            get => TMP != null ? TMP.fontSize : 16f;
            set { if (TMP != null) TMP.fontSize = value; }
        }
    }

    /// <summary>
    /// Wrapper for buttons inside UI panels.
    /// </summary>
    public class UIButton
    {
        public GameObject GameObject { get; }
        public Button Button { get; }
        public TextMeshProUGUI TMP { get; }

        public UIButton(GameObject go, Button button, TextMeshProUGUI tmp)
        {
            GameObject = go;
            Button = button;
            TMP = tmp;
        }

        /// <summary>Gets or sets the button label text.</summary>
        public string Text
        {
            get => TMP != null ? TMP.text : string.Empty;
            set { if (TMP != null) TMP.text = value; }
        }

        /// <summary>Gets or sets whether the button is clickable.</summary>
        public bool Interactable
        {
            get => Button != null && Button.interactable;
            set { if (Button != null) Button.interactable = value; }
        }
    }

    /// <summary>
    /// Wrapper for toggles / checkboxes inside UI panels.
    /// </summary>
    public class UIToggle
    {
        public GameObject GameObject { get; }
        public Toggle Toggle { get; }
        public TextMeshProUGUI TMP { get; }
        public Image Checkmark { get; }

        public UIToggle(GameObject go, Toggle toggle, TextMeshProUGUI tmp, Image checkmark)
        {
            GameObject = go;
            Toggle = toggle;
            TMP = tmp;
            Checkmark = checkmark;
        }

        /// <summary>Gets or sets the current checked status.</summary>
        public bool Value
        {
            get => Toggle != null && Toggle.isOn;
            set { if (Toggle != null) Toggle.isOn = value; }
        }

        /// <summary>Gets or sets the toggle label text.</summary>
        public string Text
        {
            get => TMP != null ? TMP.text : string.Empty;
            set { if (TMP != null) TMP.text = value; }
        }

        /// <summary>Gets or sets whether the toggle is interactable.</summary>
        public bool Interactable
        {
            get => Toggle != null && Toggle.interactable;
            set { if (Toggle != null) Toggle.interactable = value; }
        }
    }

    /// <summary>
    /// Wrapper for sliders inside UI panels.
    /// </summary>
    public class UISlider
    {
        public GameObject GameObject { get; }
        public Slider Slider { get; }
        public TextMeshProUGUI LabelTMP { get; }
        public TextMeshProUGUI ValueTMP { get; }
        public string ValueFormat { get; set; } = "F1";

        public UISlider(GameObject go, Slider slider, TextMeshProUGUI labelTmp, TextMeshProUGUI valueTmp, string format)
        {
            GameObject = go;
            Slider = slider;
            LabelTMP = labelTmp;
            ValueTMP = valueTmp;
            ValueFormat = format;
        }

        /// <summary>Gets or sets the current slider value.</summary>
        public float Value
        {
            get => Slider != null ? Slider.value : 0f;
            set
            {
                if (Slider != null)
                {
                    Slider.value = value;
                    UpdateValueDisplay(value);
                }
            }
        }

        /// <summary>Gets or sets the slider label text.</summary>
        public string Label
        {
            get => LabelTMP != null ? LabelTMP.text : string.Empty;
            set { if (LabelTMP != null) LabelTMP.text = value; }
        }

        /// <summary>Gets or sets whether the slider is interactable.</summary>
        public bool Interactable
        {
            get => Slider != null && Slider.interactable;
            set { if (Slider != null) Slider.interactable = value; }
        }

        internal void UpdateValueDisplay(float val)
        {
            if (ValueTMP != null)
            {
                ValueTMP.text = val.ToString(ValueFormat);
            }
        }
    }

    /// <summary>
    /// Horizontal element layout group for placing buttons or inputs side by side.
    /// </summary>
    public class UIHorizontalGroup
    {
        public GameObject GameObject { get; }
        public RectTransform RectTransform { get; }

        public UIHorizontalGroup(GameObject go)
        {
            GameObject = go;
            RectTransform = go.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Adds a styled button into this horizontal group row.
        /// </summary>
        public UIButton AddButton(string text, Action onClick, Color? accentColor = null, float height = 36f)
        {
            return UIPanel.BuildButton(GameObject.transform, text, onClick, accentColor, height);
        }

        /// <summary>
        /// Adds a label into this horizontal row.
        /// </summary>
        public UILabel AddLabel(string text, float fontSize = 16f, Color? color = null)
        {
            return UIPanel.BuildLabel(GameObject.transform, text, fontSize, color ?? UIColors.TextWhite, TextAlignmentOptions.Center);
        }
    }

    /// <summary>
    /// Represents a styled, draggable cyber window panel with flexible anchoring and element building.
    /// </summary>
    public class UIPanel
    {
        /// <summary>The root GameObject of this panel.</summary>
        public GameObject GameObject { get; }

        /// <summary>The RectTransform of this panel.</summary>
        public RectTransform RectTransform { get; }

        /// <summary>The container transform where content elements are appended.</summary>
        public RectTransform ContentContainer { get; }

        /// <summary>The title label text component.</summary>
        public TextMeshProUGUI TitleTMP { get; }

        /// <summary>The close button component.</summary>
        public Button CloseButton { get; }

        /// <summary>Event invoked when the panel is closed via the X button or Hide().</summary>
        public event Action OnClose;

        /// <summary>Gets or sets the window title.</summary>
        public string Title
        {
            get => TitleTMP != null ? TitleTMP.text : string.Empty;
            set { if (TitleTMP != null) TitleTMP.text = value; }
        }

        /// <summary>Gets or sets whether this window is visible.</summary>
        public bool IsVisible
        {
            get => GameObject != null && GameObject.activeSelf;
            set
            {
                if (GameObject != null)
                {
                    GameObject.SetActive(value);
                    if (!value) OnClose?.Invoke();
                }
            }
        }

        /// <summary>Gets or sets the anchored position on the screen.</summary>
        public Vector2 Position
        {
            get => RectTransform != null ? RectTransform.anchoredPosition : Vector2.zero;
            set { if (RectTransform != null) RectTransform.anchoredPosition = value; }
        }

        /// <summary>Gets or sets the panel width in pixels.</summary>
        public float Width
        {
            get => RectTransform != null ? RectTransform.sizeDelta.x : 0f;
            set
            {
                if (RectTransform != null)
                    RectTransform.sizeDelta = new Vector2(value, RectTransform.sizeDelta.y);
            }
        }

        /// <summary>Gets or sets the panel height in pixels.</summary>
        public float Height
        {
            get => RectTransform != null ? RectTransform.sizeDelta.y : 0f;
            set
            {
                if (RectTransform != null)
                    RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, value);
            }
        }

        internal UIPanel(GameObject root, RectTransform rect, RectTransform content, TextMeshProUGUI titleTmp, Button closeBtn)
        {
            GameObject = root;
            RectTransform = rect;
            ContentContainer = content;
            TitleTMP = titleTmp;
            CloseButton = closeBtn;

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(new Action(() => Hide()));
            }
        }

        /// <summary>Shows the panel.</summary>
        public UIPanel Show()
        {
            IsVisible = true;
            return this;
        }

        /// <summary>Hides the panel.</summary>
        public UIPanel Hide()
        {
            IsVisible = false;
            return this;
        }

        /// <summary>Toggles panel visibility.</summary>
        public UIPanel Toggle()
        {
            IsVisible = !IsVisible;
            return this;
        }

        /// <summary>Destroys the panel GameObject from the scene.</summary>
        public void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
            }
        }

        /// <summary>
        /// Sets the panel anchor preset with an optional pixel offset.
        /// Supports Center, TopLeft, BottomLeft, TopRight, etc.
        /// </summary>
        public UIPanel SetAnchor(UIAnchor anchor, Vector2? offset = null)
        {
            if (RectTransform == null) return this;

            Vector2 anchorVector;
            Vector2 pivotVector;

            switch (anchor)
            {
                case UIAnchor.TopLeft:
                    anchorVector = new Vector2(0f, 1f);
                    pivotVector = new Vector2(0f, 1f);
                    break;
                case UIAnchor.TopCenter:
                    anchorVector = new Vector2(0.5f, 1f);
                    pivotVector = new Vector2(0.5f, 1f);
                    break;
                case UIAnchor.TopRight:
                    anchorVector = new Vector2(1f, 1f);
                    pivotVector = new Vector2(1f, 1f);
                    break;
                case UIAnchor.MiddleLeft:
                    anchorVector = new Vector2(0f, 0.5f);
                    pivotVector = new Vector2(0f, 0.5f);
                    break;
                case UIAnchor.MiddleRight:
                    anchorVector = new Vector2(1f, 0.5f);
                    pivotVector = new Vector2(1f, 0.5f);
                    break;
                case UIAnchor.BottomLeft:
                    anchorVector = new Vector2(0f, 0f);
                    pivotVector = new Vector2(0f, 0f);
                    break;
                case UIAnchor.BottomCenter:
                    anchorVector = new Vector2(0.5f, 0f);
                    pivotVector = new Vector2(0.5f, 0f);
                    break;
                case UIAnchor.BottomRight:
                    anchorVector = new Vector2(1f, 0f);
                    pivotVector = new Vector2(1f, 0f);
                    break;
                case UIAnchor.Stretch:
                    RectTransform.anchorMin = Vector2.zero;
                    RectTransform.anchorMax = Vector2.one;
                    RectTransform.pivot = new Vector2(0.5f, 0.5f);
                    if (offset.HasValue) RectTransform.anchoredPosition = offset.Value;
                    return this;
                case UIAnchor.Center:
                default:
                    anchorVector = new Vector2(0.5f, 0.5f);
                    pivotVector = new Vector2(0.5f, 0.5f);
                    break;
            }

            RectTransform.anchorMin = anchorVector;
            RectTransform.anchorMax = anchorVector;
            RectTransform.pivot = pivotVector;

            if (offset.HasValue)
            {
                RectTransform.anchoredPosition = offset.Value;
            }

            return this;
        }

        /// <summary>
        /// Sets custom min and max anchors, pivot, and position for total placement freedom.
        /// </summary>
        public UIPanel SetAnchor(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2? anchoredPosition = null)
        {
            if (RectTransform == null) return this;
            RectTransform.anchorMin = anchorMin;
            RectTransform.anchorMax = anchorMax;
            RectTransform.pivot = pivot;
            if (anchoredPosition.HasValue)
            {
                RectTransform.anchoredPosition = anchoredPosition.Value;
            }
            return this;
        }

        /// <summary>Sets exact pixel coordinates for the panel.</summary>
        public UIPanel SetPosition(float x, float y)
        {
            Position = new Vector2(x, y);
            return this;
        }

        /// <summary>Sets exact pixel coordinates for the panel.</summary>
        public UIPanel SetPosition(Vector2 position)
        {
            Position = position;
            return this;
        }

        /// <summary>Sets width and height in pixels.</summary>
        public UIPanel SetSize(float width, float height)
        {
            if (RectTransform != null)
                RectTransform.sizeDelta = new Vector2(width, height);
            return this;
        }

        /// <summary>Sets width and height in pixels.</summary>
        public UIPanel SetSize(Vector2 size)
        {
            if (RectTransform != null)
                RectTransform.sizeDelta = size;
            return this;
        }

        /// <summary>
        /// Adds a styled section header title inside the panel content.
        /// </summary>
        public UILabel AddHeader(string text, Color? color = null)
        {
            return BuildLabel(ContentContainer, text, 20f, color ?? UIColors.Cyan, TextAlignmentOptions.Left, true);
        }

        /// <summary>
        /// Adds a text label inside the panel content.
        /// </summary>
        public UILabel AddLabel(string text, float fontSize = 16f, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            return BuildLabel(ContentContainer, text, fontSize, color ?? UIColors.TextWhite, alignment, false);
        }

        /// <summary>
        /// Adds a styled cyber button into the panel content.
        /// </summary>
        public UIButton AddButton(string text, Action onClick, Color? accentColor = null, float height = 40f)
        {
            return BuildButton(ContentContainer, text, onClick, accentColor, height);
        }

        /// <summary>
        /// Adds a styled toggle / checkbox into the panel content.
        /// </summary>
        public UIToggle AddToggle(string text, bool initialValue, Action<bool> onValueChanged)
        {
            var rowObj = new GameObject("ToggleRow_" + text);
            rowObj.transform.SetParent(ContentContainer, false);

            var rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 32f);

            var le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 32f;
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;

            var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = text;
            labelTmp.fontSize = 15f;
            labelTmp.color = UIColors.TextWhite;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (UI.GameFont != null) labelTmp.font = UI.GameFont;

            var labelLe = labelObj.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;

            // Toggle Box
            var toggleBox = new GameObject("ToggleBox");
            toggleBox.transform.SetParent(rowObj.transform, false);
            var toggleRect = toggleBox.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(26f, 26f);

            var boxLe = toggleBox.AddComponent<LayoutElement>();
            boxLe.minWidth = 26f;
            boxLe.preferredWidth = 26f;
            boxLe.minHeight = 26f;
            boxLe.preferredHeight = 26f;

            var boxImg = toggleBox.AddComponent<Image>();
            boxImg.sprite = UISpriteFactory.ToggleBackground;
            boxImg.type = Image.Type.Sliced;
            boxImg.color = Color.white;

            // Checkmark Inside
            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(toggleBox.transform, false);
            var checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.sizeDelta = Vector2.zero;

            var checkImg = checkObj.AddComponent<Image>();
            checkImg.sprite = UISpriteFactory.White;
            checkImg.color = UIColors.Cyan;

            var toggle = rowObj.AddComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic = checkImg;
            toggle.isOn = initialValue;

            if (onValueChanged != null)
            {
                toggle.onValueChanged.AddListener(new Action<bool>(val =>
                {
                    try { onValueChanged.Invoke(val); }
                    catch (Exception ex) { Debug.LogError($"[RombyLib.UI] Toggle callback error: {ex}"); }
                }));
            }

            return new UIToggle(rowObj, toggle, labelTmp, checkImg);
        }

        /// <summary>
        /// Adds a styled slider with readout label into the panel content.
        /// </summary>
        public UISlider AddSlider(string label, float min, float max, float initialValue, Action<float> onValueChanged, string format = "F1", float height = 44f)
        {
            var rowObj = new GameObject("SliderRow_" + label);
            rowObj.transform.SetParent(ContentContainer, false);

            var le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;

            var vlg = rowObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header Row (Label on left, value on right)
            var headRow = new GameObject("HeadRow");
            headRow.transform.SetParent(rowObj.transform, false);
            var hlg = headRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;

            var headLe = headRow.AddComponent<LayoutElement>();
            headLe.preferredHeight = 18f;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(headRow.transform, false);
            var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 14f;
            labelTmp.color = UIColors.TextWhite;
            if (UI.GameFont != null) labelTmp.font = UI.GameFont;
            var lLe = labelObj.AddComponent<LayoutElement>();
            lLe.flexibleWidth = 1f;

            var valObj = new GameObject("Value");
            valObj.transform.SetParent(headRow.transform, false);
            var valTmp = valObj.AddComponent<TextMeshProUGUI>();
            valTmp.text = initialValue.ToString(format);
            valTmp.fontSize = 14f;
            valTmp.color = UIColors.Cyan;
            valTmp.alignment = TextAlignmentOptions.Right;
            if (UI.GameFont != null) valTmp.font = UI.GameFont;

            // Slider Track
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(rowObj.transform, false);
            var sRect = sliderObj.AddComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(0f, 18f);

            var trackLe = sliderObj.AddComponent<LayoutElement>();
            trackLe.preferredHeight = 18f;
            trackLe.flexibleWidth = 1f;

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = UISpriteFactory.SliderTrack;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.sizeDelta = new Vector2(-10f, 0f);

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = UISpriteFactory.SliderFill;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = UIColors.Cyan;

            // Handle Area
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-16f, 0f);

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            var handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = UISpriteFactory.SliderKnob;
            handleImg.color = Color.white;

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initialValue;

            var sliderWrapper = new UISlider(rowObj, slider, labelTmp, valTmp, format);

            slider.onValueChanged.AddListener(new Action<float>(val =>
            {
                sliderWrapper.UpdateValueDisplay(val);
                try { onValueChanged?.Invoke(val); }
                catch (Exception ex) { Debug.LogError($"[RombyLib.UI] Slider callback error: {ex}"); }
            }));

            return sliderWrapper;
        }

        /// <summary>
        /// Adds a subtle horizontal divider line separating sections.
        /// </summary>
        public GameObject AddDivider(Color? color = null, float height = 2f)
        {
            var divObj = new GameObject("Divider");
            divObj.transform.SetParent(ContentContainer, false);

            var le = divObj.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;

            var img = divObj.AddComponent<Image>();
            img.sprite = UISpriteFactory.White;
            img.color = color ?? UIColors.Divider;

            return divObj;
        }

        /// <summary>
        /// Adds vertical spacing inside the content.
        /// </summary>
        public GameObject AddSpace(float height = 10f)
        {
            var spaceObj = new GameObject("Space");
            spaceObj.transform.SetParent(ContentContainer, false);

            var le = spaceObj.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            return spaceObj;
        }

        /// <summary>
        /// Adds a horizontal layout row to place buttons or items side by side.
        /// </summary>
        public UIHorizontalGroup AddHorizontalGroup(Action<UIHorizontalGroup> build, float spacing = 8f)
        {
            var groupObj = new GameObject("HorizontalGroup");
            groupObj.transform.SetParent(ContentContainer, false);

            var hlg = groupObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            var le = groupObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            var group = new UIHorizontalGroup(groupObj);
            build?.Invoke(group);
            return group;
        }

        /// <summary>
        /// Appends an arbitrary custom GameObject to the content hierarchy.
        /// </summary>
        public GameObject AddCustom(GameObject customObject)
        {
            if (customObject != null)
            {
                customObject.transform.SetParent(ContentContainer, false);
            }
            return customObject;
        }

        internal static UILabel BuildLabel(Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment, bool bold = false)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = bold ? $"<b>{text}</b>" : text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.lineSpacing = 1.15f;
            if (UI.GameFont != null) tmp.font = UI.GameFont;

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            return new UILabel(go, tmp);
        }

        internal static UIButton BuildButton(Transform parent, string text, Action onClick, Color? accentColor, float height)
        {
            var go = new GameObject("Button_" + text);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;

            var img = go.AddComponent<Image>();
            img.sprite = UISpriteFactory.ButtonNormal;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Hover and pressed color states
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = accentColor ?? UIColors.Cyan;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            // Button Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{text}</b>";
            tmp.fontSize = 16f;
            tmp.color = UIColors.TextWhite;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (UI.GameFont != null) tmp.font = UI.GameFont;

            if (onClick != null)
            {
                btn.onClick.AddListener(new Action(() =>
                {
                    try { onClick.Invoke(); }
                    catch (Exception ex) { Debug.LogError($"[RombyLib.UI] Button onClick error: {ex}"); }
                }));
            }

            return new UIButton(go, btn, tmp);
        }
    }

    /// <summary>
    /// Core UI system for RombyLib providing seamless, native-styled cyberpunk HUDs and menus.
    /// </summary>
    public static class UI
    {
        private static GameObject _canvasObject;
        private static Canvas _canvas;
        private static CanvasScaler _canvasScaler;
        private static RectTransform _canvasRect;
        private static TMP_FontAsset _cachedFont;
        private static TMP_FontAsset _cachedOutlinedFont;
        private static bool _isCustomTypesRegistered;
        private static readonly Dictionary<NotificationPosition, GameObject> _activeAnnouncements = new Dictionary<NotificationPosition, GameObject>();

        /// <summary>
        /// Gets the singleton screen-space overlay Canvas for all RombyLib UI elements.
        /// Automatically instantiated and persisted across scene changes.
        /// </summary>
        public static Canvas Canvas
        {
            get
            {
                EnsureCanvas();
                return _canvas;
            }
        }

        /// <summary>
        /// Gets the root RectTransform of the singleton UI Canvas.
        /// </summary>
        public static RectTransform CanvasRect
        {
            get
            {
                EnsureCanvas();
                return _canvasRect;
            }
        }

        /// <summary>
        /// Gets Rombykon's native Cactron SDF font asset if loaded, or the primary loaded TMP font.
        /// </summary>
        /// <summary>
        /// Checks if a string contains any Cyrillic characters.
        /// </summary>
        public static bool ContainsCyrillic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '\u0400' && c <= '\u04FF') return true;
            }
            return false;
        }

        /// <summary>
        /// Gets Rombykon's native stylized font asset (WDXLLubrifontSC-Regular SDF Borde Gordo or Cactron SDF).
        /// </summary>
        public static TMP_FontAsset GameFont
        {
            get
            {
                if (_cachedFont != null && _cachedFont.Pointer != IntPtr.Zero && !_cachedFont.WasCollected)
                    return _cachedFont;

                try
                {
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        // Priority 0: WDXLLubrifont (The game's native stylized Russian/Cyrillic font)
                        foreach (var font in fonts)
                        {
                            if (font == null || font.Pointer == IntPtr.Zero || font.WasCollected) continue;
                            if (font.name.IndexOf("WDXL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                font.name.IndexOf("Lubri", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _cachedFont = font;
                                return _cachedFont;
                            }
                        }

                        // Priority 1: Cactron SDF non-outline (Latin stylized font)
                        foreach (var font in fonts)
                        {
                            if (font == null || font.Pointer == IntPtr.Zero || font.WasCollected) continue;
                            if (font.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            if (font.name.IndexOf("Cactron", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _cachedFont = font;
                                return _cachedFont;
                            }
                        }

                        // Priority 2: Any non-Liberation font
                        foreach (var font in fonts)
                        {
                            if (font == null || font.Pointer == IntPtr.Zero || font.WasCollected) continue;
                            if (font.name.IndexOf("Liberation", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                _cachedFont = font;
                                return _cachedFont;
                            }
                        }

                        // Priority 3: Fallback to first available font
                        foreach (var font in fonts)
                        {
                            if (font != null && font.Pointer != IntPtr.Zero && !font.WasCollected)
                            {
                                _cachedFont = font;
                                return _cachedFont;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.UI] TMP font lookup exception: {ex.Message}");
                }

                return null;
            }
        }

        /// <summary>
        /// Gets Rombykon's native outlined font asset (Cactron SDF 1 Mas Outline or WDXLLubrifont Borde Gordo).
        /// </summary>
        public static TMP_FontAsset OutlinedFont
        {
            get
            {
                if (_cachedOutlinedFont != null && _cachedOutlinedFont.Pointer != IntPtr.Zero && !_cachedOutlinedFont.WasCollected)
                    return _cachedOutlinedFont;

                try
                {
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts != null)
                    {
                        // Priority 0: Cactron Mas Outline
                        foreach (var font in fonts)
                        {
                            if (font == null || font.Pointer == IntPtr.Zero || font.WasCollected) continue;
                            if (font.name.IndexOf("Mas Outline", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (font.name.IndexOf("Cactron", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                 font.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                _cachedOutlinedFont = font;
                                return _cachedOutlinedFont;
                            }
                        }

                        // Priority 1: WDXLLubrifont (already thick border)
                        foreach (var font in fonts)
                        {
                            if (font == null || font.Pointer == IntPtr.Zero || font.WasCollected) continue;
                            if (font.name.IndexOf("WDXL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                font.name.IndexOf("Lubri", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _cachedOutlinedFont = font;
                                return _cachedOutlinedFont;
                            }
                        }
                    }
                }
                catch { }

                return GameFont;
            }
        }

        /// <summary>
        /// Creates a styled, draggable window panel matching Rombykon's native aesthetic.
        /// </summary>
        /// <param name="title">Window title text.</param>
        /// <param name="width">Width in pixels (default 420).</param>
        /// <param name="height">Height in pixels (default 500).</param>
        /// <param name="anchor">Screen anchor preset (default Center).</param>
        /// <param name="offset">Optional anchored position pixel offset.</param>
        /// <returns>A new UIPanel builder instance.</returns>
        public static UIPanel CreatePanel(string title, float width = 420f, float height = 500f, UIAnchor anchor = UIAnchor.Center, Vector2? offset = null)
        {
            EnsureCanvas();

            // Root Panel GameObject
            var panelObj = new GameObject("RombyLib_Panel_" + title);
            panelObj.transform.SetParent(_canvasObject.transform, false);

            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(width, height);

            var panelImg = panelObj.AddComponent<Image>();
            panelImg.sprite = UISpriteFactory.Panel;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;

            // Header Bar
            var headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(panelObj.transform, false);

            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 48f);
            headerRect.anchoredPosition = Vector2.zero;

            var headerImg = headerObj.AddComponent<Image>();
            headerImg.sprite = UISpriteFactory.Header;
            headerImg.type = Image.Type.Sliced;
            headerImg.color = Color.white;

            // Make panel draggable by dragging header
            var dragHandler = headerObj.AddComponent<UIDragHandler>();
            dragHandler.TargetRect = panelRect;
            dragHandler.CanvasRect = _canvasRect;
            dragHandler.HeaderRect = headerRect;

            // Title Text
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.sizeDelta = new Vector2(-90f, 0f);
            titleRect.anchoredPosition = new Vector2(18f, 0f);

            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"<b>{title.ToUpperInvariant()}</b>";
            titleTmp.fontSize = 19f;
            titleTmp.color = UIColors.Cyan;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.raycastTarget = false;
            if (GameFont != null) titleTmp.font = GameFont;

            // Close Button [X]
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(headerObj.transform, false);

            var closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(30f, 30f);
            closeRect.anchoredPosition = new Vector2(-12f, 0f);

            var closeImg = closeObj.AddComponent<Image>();
            closeImg.sprite = UISpriteFactory.ButtonNormal;
            closeImg.type = Image.Type.Sliced;
            closeImg.color = Color.white;

            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var closeColors = closeBtn.colors;
            closeColors.highlightedColor = UIColors.Red;
            closeColors.pressedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            closeBtn.colors = closeColors;

            var closeTextObj = new GameObject("CloseIcon");
            closeTextObj.transform.SetParent(closeObj.transform, false);
            var ctRect = closeTextObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            var ctTmp = closeTextObj.AddComponent<TextMeshProUGUI>();
            ctTmp.text = "<b>✕</b>";
            ctTmp.fontSize = 16f;
            ctTmp.color = UIColors.TextWhite;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.raycastTarget = false;
            // Use default TMP font for crisp cross glyph (not Cactron which renders X as H)

            // Header Separator Line
            var lineObj = new GameObject("HeaderLine");
            lineObj.transform.SetParent(headerObj.transform, false);
            var lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.sizeDelta = new Vector2(-4f, 1.5f);
            lineRect.anchoredPosition = new Vector2(0f, 0f);
            var lineImg = lineObj.AddComponent<Image>();
            lineImg.sprite = UISpriteFactory.White;
            lineImg.color = UIColors.BorderCyan;

            // Scrollable Content Area
            var scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(panelObj.transform, false);

            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(-28f, -68f);
            scrollRect.anchoredPosition = new Vector2(0f, -28f);

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;

            // Viewport Mask
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);

            var vpRect = viewportObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            var vpMask = viewportObj.AddComponent<RectMask2D>();
            scroll.viewport = vpRect;

            // Content Container
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 12, 14);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            var panel = new UIPanel(panelObj, panelRect, contentRect, titleTmp, closeBtn);
            panel.SetAnchor(anchor, offset);

            return panel;
        }

        /// <summary>
        /// Creates a panel with exact screen pixel coordinates.
        /// </summary>
        public static UIPanel CreatePanel(string title, float width, float height, float x, float y)
        {
            return CreatePanel(title, width, height, UIAnchor.Center, new Vector2(x, y));
        }

        private static readonly Dictionary<string, AudioClip> _cachedAudioClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private static AudioSource _uiAudioSource;

        private static void EnsureAudioClipsCached()
        {
            if (_cachedAudioClips.Count > 0) return;
            try
            {
                var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                if (clips != null)
                {
                    foreach (var c in clips)
                    {
                        if (c != null && !string.IsNullOrEmpty(c.name))
                        {
                            _cachedAudioClips[c.name] = c;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Resolves an in-game AudioClip corresponding to the given preset or custom sound name.
        /// </summary>
        public static AudioClip ResolveClip(NotificationSound sound, string customSoundName = null)
        {
            EnsureAudioClipsCached();

            if (!string.IsNullOrEmpty(customSoundName))
            {
                if (_cachedAudioClips.TryGetValue(customSoundName, out var customClip)) return customClip;
                foreach (var kvp in _cachedAudioClips)
                {
                    if (kvp.Key.IndexOf(customSoundName, StringComparison.OrdinalIgnoreCase) >= 0)
                        return kvp.Value;
                }
            }

            if (sound == NotificationSound.None) return null;

            string[] keywords;
            switch (sound)
            {
                case NotificationSound.Coin:
                    keywords = new[] { "sonidovender", "throwmoneysound", "sonidorejilla", "moneda", "coin", "dinero", "oro", "gold", "cash" };
                    break;
                case NotificationSound.Success:
                    keywords = new[] { "sonidoexito", "sonidopremio", "sonidoestrella", "sonidoconfeti", "sonidonuevorecord", "win", "victoria", "success" };
                    break;
                case NotificationSound.Click:
                    keywords = new[] { "sonidobotonmasmenos", "sonidoboton", "clicksoundclip", "sonidohover", "click", "boton", "button", "select" };
                    break;
                case NotificationSound.Warning:
                    keywords = new[] { "sonidoerror", "sonidofallo", "error", "hurt", "golpe", "damage", "fail", "wrong", "alert" };
                    break;
                case NotificationSound.Magic:
                    keywords = new[] { "sonidoestrella", "sonidocarta", "sonidoforma", "cristalsonidodialogo", "cristal", "crystal", "carta", "card" };
                    break;
                case NotificationSound.Default:
                default:
                    keywords = new[] { "sonidoseleccionvisual", "sonidocarta", "sonidobotonmasmenos", "sonidopop", "pop", "sonidoabrirpanel", "sonidorejilla", "sonidoboton" };
                    break;
            }

            foreach (var kw in keywords)
            {
                foreach (var kvp in _cachedAudioClips)
                {
                    if (kvp.Key.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        return kvp.Value;
                }
            }

            // Fallback: return any available game sound clip
            foreach (var kvp in _cachedAudioClips)
            {
                return kvp.Value;
            }

            return null;
        }

        /// <summary>
        /// Plays an in-game sound effect preset matching Rombykon's native audio effects.
        /// Volume automatically respects the player's in-game SFX settings.
        /// </summary>
        public static void PlaySound(NotificationSound sound, float volume = 1f)
        {
            // Sounds disabled per user request
        }

        public static void PlaySound(string clipName, float volume = 1f)
        {
            // Sounds disabled per user request
        }

        public static void PlaySound(AudioClip clip, float volume = 1f)
        {
            // Sounds disabled per user request
        }

        /// <summary>
        /// Displays an authentic in-game announcement notification with smooth floating animations,
        /// matching Rombykon's native announcement aesthetics (as seen in NPC and wave unlock messages).
        /// </summary>
        /// <param name="title">Main announcement title.</param>
        /// <param name="subtitle">Optional secondary message or description.</param>
        /// <param name="duration">Duration in seconds (default 3.5s).</param>
        /// <param name="titleColor">Color of the title text (default cyan #50E0FF).</param>
        /// <param name="position">Screen location (Bottom, Top, Center; default Bottom).</param>
        /// <param name="sound">Sound effect preset to play on display (default: None).</param>
        /// <param name="customSoundName">Optional custom game audio clip name to play.</param>
        public static void ShowAnnouncement(
            string title,
            string subtitle = null,
            float duration = 3.5f,
            Color? titleColor = null,
            NotificationPosition position = NotificationPosition.Bottom,
            NotificationSound sound = NotificationSound.None,
            string customSoundName = null)
        {
            try
            {
                EnsureCanvas();

                // Anti-overlap: dismiss existing notification at this position so they don't clobber
                if (_activeAnnouncements.TryGetValue(position, out var existing) && existing != null && existing.Pointer != IntPtr.Zero && !existing.WasCollected)
                {
                    UnityEngine.Object.Destroy(existing);
                }

                var notifObj = new GameObject("RombyLib_Announcement_" + title);
                notifObj.transform.SetParent(_canvasObject.transform, false);
                _activeAnnouncements[position] = notifObj;

                var cg = notifObj.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;

                var rect = notifObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1200f, 160f);

                Vector2 startPos, targetPos, endPos;
                switch (position)
                {
                    case NotificationPosition.Top:
                        rect.anchorMin = new Vector2(0.5f, 1f);
                        rect.anchorMax = new Vector2(0.5f, 1f);
                        rect.pivot = new Vector2(0.5f, 1f);
                        targetPos = new Vector2(0f, -200f);
                        startPos = new Vector2(0f, -170f);
                        endPos = new Vector2(0f, -220f);
                        break;
                    case NotificationPosition.Center:
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        targetPos = new Vector2(0f, 0f);
                        startPos = new Vector2(0f, -25f);
                        endPos = new Vector2(0f, 25f);
                        break;
                    case NotificationPosition.Bottom:
                    default:
                        rect.anchorMin = new Vector2(0.5f, 0f);
                        rect.anchorMax = new Vector2(0.5f, 0f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        targetPos = new Vector2(0f, 155f);
                        startPos = new Vector2(0f, 125f);
                        endPos = new Vector2(0f, 175f);
                        break;
                }

                rect.anchoredPosition = startPos;

                var vlg = notifObj.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.spacing = 6f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var csf = notifObj.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Font setup: If text contains Cyrillic characters, prioritize GameFont (WDXLLubrifont) to avoid missing glyphs
                bool hasCyrillic = ContainsCyrillic(title) || ContainsCyrillic(subtitle);
                var titleFont = (hasCyrillic ? GameFont : OutlinedFont) ?? GameFont;
                var subFont = GameFont ?? OutlinedFont;

                // Title Text
                var titleObj = new GameObject("Title");
                titleObj.transform.SetParent(notifObj.transform, false);

                var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
                titleTmp.text = $"<b>{title}</b>";
                titleTmp.fontSize = 42f;
                titleTmp.color = titleColor ?? new Color(0.6f, 0.906f, 1f, 1f); // #99E7FF Rombykon native ice cyan
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.enableWordWrapping = true;
                titleTmp.raycastTarget = false;
                if (titleFont != null) titleTmp.font = titleFont;

                // Subtitle Text (if provided)
                if (!string.IsNullOrEmpty(subtitle))
                {
                    var subObj = new GameObject("Subtitle");
                    subObj.transform.SetParent(notifObj.transform, false);

                    var subTmp = subObj.AddComponent<TextMeshProUGUI>();
                    subTmp.text = $"<b>{subtitle}</b>";
                    subTmp.fontSize = 32f;
                    subTmp.color = new Color(0.92f, 0.97f, 1f, 1f); // Crisp bright ice-cyan/white
                    subTmp.alignment = TextAlignmentOptions.Center;
                    subTmp.enableWordWrapping = true;
                    subTmp.raycastTarget = false;
                    if (subFont != null) subTmp.font = subFont;
                }

                // Attach animator for smooth float & fade
                var anim = notifObj.AddComponent<UINotificationAnimator>();
                anim.CanvasGroup = cg;
                anim.RectTransform = rect;
                anim.Duration = duration;
                anim.StartPosition = startPos;
                anim.TargetPosition = targetPos;
                anim.EndPosition = endPos;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.UI] Failed to display announcement: {ex}");
            }
        }

        /// <summary>
        /// Displays a notification message in Rombykon's native announcement style.
        /// </summary>
        /// <param name="message">Notification text.</param>
        /// <param name="duration">Duration in seconds (default 3.5s).</param>
        /// <param name="accentColor">Accent color for message text.</param>
        /// <param name="position">Screen location (default Bottom).</param>
        /// <param name="sound">Sound effect preset to play on display (default: Default).</param>
        /// <param name="customSoundName">Optional custom game audio clip name to play.</param>
        public static void ShowNotification(
            string message,
            float duration = 3.5f,
            Color? accentColor = null,
            NotificationPosition position = NotificationPosition.Bottom,
            NotificationSound sound = NotificationSound.None,
            string customSoundName = null)
        {
            ShowAnnouncement(message, null, duration, accentColor, position, sound, customSoundName);
        }

        private static void EnsureCanvas()
        {
            if (!_isCustomTypesRegistered)
            {
                try
                {
                    ClassInjector.RegisterTypeInIl2Cpp<UIDragHandler>();
                    ClassInjector.RegisterTypeInIl2Cpp<UINotificationAnimator>();
                    _isCustomTypesRegistered = true;
                }
                catch
                {
                    // Already registered or injected
                    _isCustomTypesRegistered = true;
                }
            }

            if (_canvasObject != null && _canvasObject.Pointer != IntPtr.Zero && !_canvasObject.WasCollected)
            {
                return;
            }

            _canvasObject = new GameObject("RombyLib_Canvas");
            _canvasObject.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(_canvasObject);

            _canvas = _canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            _canvasScaler = _canvasObject.AddComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f;

            _canvasObject.AddComponent<GraphicRaycaster>();
            _canvasRect = _canvasObject.GetComponent<RectTransform>();
        }
    }
}
