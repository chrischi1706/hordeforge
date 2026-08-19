using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HordeForge.Unity.UI
{
    /// <summary>
    /// Baukasten fuer die Oberflaeche. Das gesamte HUD entsteht zur Laufzeit aus Code –
    /// dadurch gibt es keine Prefabs, keine importierten Schriften und nichts, was beim
    /// Oeffnen des Projekts fehlen koennte.
    /// </summary>
    public static class UiBuilder
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        public static readonly Color SubPanelColor = new Color(0.13f, 0.15f, 0.19f, 0.95f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.24f, 0.31f, 1f);
        public static readonly Color ButtonDisabledColor = new Color(0.16f, 0.17f, 0.19f, 1f);
        public static readonly Color AccentColor = new Color(0.98f, 0.78f, 0.30f);
        public static readonly Color WarningColor = new Color(0.98f, 0.55f, 0.25f);
        public static readonly Color AlertColor = new Color(0.96f, 0.35f, 0.32f);
        public static readonly Color TextColor = new Color(0.90f, 0.92f, 0.95f);
        public static readonly Color MutedColor = new Color(0.62f, 0.66f, 0.72f);

        private static Font _font;

        /// <summary>
        /// Unity liefert eine eingebaute Schrift mit. Der Name hat sich zwischen den
        /// Versionen geaendert, deshalb der Fallback.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _font;
            }
        }

        public static Canvas CreateCanvas(Transform root)
        {
            GameObject canvasObject = new GameObject("HUD", typeof(RectTransform));
            canvasObject.transform.SetParent(root, false);
            canvasObject.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem(root);
            return canvas;
        }

        /// <summary>
        /// Die Szene enthaelt genau einen Bootstrap, der das HUD einmal aufbaut –
        /// deshalb wird das EventSystem direkt erzeugt, statt nach einem vorhandenen
        /// zu suchen. Das erspart die je nach Unity-Version unterschiedlich
        /// benannten Such-APIs.
        /// </summary>
        private static void EnsureEventSystem(Transform root)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(root, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Spannt ein Rechteck relativ zu seinem Elternelement auf.</summary>
        public static RectTransform Stretch(
            RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static RectTransform CreatePanel(
            Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
        }

        public static Text CreateText(
            Transform parent, string name, string content,
            int fontSize, TextAnchor anchor, Color color, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateRect(parent, name);

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.supportRichText = true;

            return text;
        }

        /// <summary>Text, der eine ganze Flaeche ausfuellt.</summary>
        public static Text CreateStretchedText(
            Transform parent, string name, string content,
            int fontSize, TextAnchor anchor, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            FontStyle style = FontStyle.Normal)
        {
            Text text = CreateText(parent, name, content, fontSize, anchor, color, style);
            Stretch(text.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
            return text;
        }

        public static Button CreateButton(
            Transform parent, string name, string label, UnityAction onClick,
            float preferredHeight = 30f, int fontSize = 15)
        {
            RectTransform rect = CreateRect(parent, name);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.minHeight = preferredHeight;

            Text text = CreateText(
                rect, "Label", label, fontSize, TextAnchor.MiddleCenter, TextColor);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 2f), new Vector2(-8f, -2f));

            return button;
        }

        /// <summary>
        /// Scrollbarer Bereich mit senkrechter Anordnung. Liefert den Inhaltscontainer,
        /// in den Eintraege gehaengt werden.
        /// </summary>
        public static RectTransform CreateScrollList(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            float spacing = 4f)
        {
            RectTransform scrollRect = CreateRect(parent, name);
            Stretch(scrollRect, anchorMin, anchorMax, offsetMin, offsetMax);

            ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            RectTransform viewport = CreateRect(scrollRect, "Viewport");
            Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            return content;
        }

        /// <summary>Waagerechte Reihe innerhalb einer senkrechten Liste.</summary>
        public static RectTransform CreateRow(Transform parent, float height, float spacing = 4f)
        {
            RectTransform row = CreateRect(parent, "Row");

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;

            return row;
        }

        /// <summary>Ueberschrift innerhalb einer Liste.</summary>
        public static Text CreateListHeading(Transform parent, string caption)
        {
            Text text = CreateText(
                parent, "Heading_" + caption, caption, 15,
                TextAnchor.MiddleLeft, AccentColor, FontStyle.Bold);

            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 24f;
            layout.minHeight = 24f;

            return text;
        }

        /// <summary>Mehrzeiliger Eintrag innerhalb einer Liste.</summary>
        public static Text CreateListText(
            Transform parent, string name, string content, int fontSize, Color color,
            float preferredHeight)
        {
            Text text = CreateText(parent, name, content, fontSize, TextAnchor.UpperLeft, color);

            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.minHeight = preferredHeight;

            return text;
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = interactable ? ButtonColor : ButtonDisabledColor;
            }
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>Liegt der Mauszeiger ueber einem UI-Element?</summary>
        public static bool IsPointerOverUi()
        {
            EventSystem current = EventSystem.current;
            return current != null && current.IsPointerOverGameObject();
        }
    }
}
