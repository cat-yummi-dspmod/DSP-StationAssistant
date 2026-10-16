using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class ControlPanelStoragePaging
    {
        private sealed class PageState
        {
            public int Page;
            public int LastStationId = -1;
        }

        private static readonly ConditionalWeakTable<StationComponent, PageState> states = new ConditionalWeakTable<StationComponent, PageState>();
        private static FieldInfo storageUIsField;
        private static FieldInfo stationField;
        private static FieldInfo factoryField;
        private static bool fieldsTried;
        private static UIControlPanelStationInspector lastInspector;
        private static UIControlPanelStationStorage[] lastUis;
        private static Text pageLabel;
        private static RectTransform chrome;
        private static float lastWheel;
        private static float nextPageTime;

        internal static bool CursorOverPagingArea()
        {
            if (!Settings.ControlScroll || lastInspector == null || lastUis == null)
            {
                return false;
            }
            if (!lastInspector.gameObject.activeInHierarchy)
            {
                return false;
            }
            return MouseOverAny(lastUis);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), "SetData")]
        [YummiHarness.Guard("61111e7e94b232ee")]
        public static void SetData_Prefix(StationComponent __1, ref int __2)
        {
            if (!Settings.ControlScroll || __1 == null)
            {
                return;
            }
            int count = CountOf(__1);
            int pageSize = Settings.PageSize;
            if (count <= pageSize)
            {
                return;
            }
            int start = Pages.Start(StateOf(__1).Page, count, pageSize);
            int mapped = start + __2;
            if (mapped < count)
            {
                __2 = mapped;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "_OnUpdate")]
        [YummiHarness.Guard("b89aade680ae7db9")]
        public static void OnUpdate_Postfix(UIControlPanelStationInspector __instance)
        {
            if (!Settings.ControlScroll || !EnsureFields())
            {
                Hide();
                return;
            }
            StationComponent station = stationField.GetValue(__instance) as StationComponent;
            UIControlPanelStationStorage[] uis = storageUIsField.GetValue(__instance) as UIControlPanelStationStorage[];
            if (station == null || uis == null || uis.Length == 0)
            {
                Hide();
                return;
            }
            int count = CountOf(station);
            int pageSize = Settings.PageSize;
            int pages = Pages.Count(count, pageSize);
            if (pages <= 1)
            {
                lastInspector = null;
                Hide();
                return;
            }
            lastInspector = __instance;
            lastUis = uis;
            PageState state = StateOf(station);
            if (state.LastStationId != station.id)
            {
                state.LastStationId = station.id;
                state.Page = 0;
            }
            int step = 0;
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                step = 1;
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                step = -1;
            }
            else if (MouseOverAny(uis))
            {
                float wheel = Input.mouseScrollDelta.y;
                if (wheel == 0f)
                {
                    lastWheel = 0f;
                }
                else if (wheel != lastWheel && Time.unscaledTime >= nextPageTime)
                {
                    lastWheel = wheel;
                    nextPageTime = Time.unscaledTime + 0.15f;
                    step = wheel > 0f ? -1 : 1;
                }
            }
            if (step != 0)
            {
                int next = state.Page + step;
                if (next < 0)
                {
                    next = 0;
                }
                if (next >= pages)
                {
                    next = pages - 1;
                }
                if (next != state.Page)
                {
                    state.Page = next;
                    Rebind(__instance, station, uis);
                }
            }
            EnsureChrome(__instance, uis);
            if (pageLabel != null)
            {
                pageLabel.text = (state.Page + 1) + "/" + pages;
            }
            if (chrome != null && !chrome.gameObject.activeSelf)
            {
                chrome.gameObject.SetActive(true);
            }
        }

        private static void Rebind(UIControlPanelStationInspector inspector, StationComponent station, UIControlPanelStationStorage[] uis)
        {
            PlanetFactory factory = factoryField.GetValue(inspector) as PlanetFactory;
            if (factory == null)
            {
                return;
            }
            int count = CountOf(station);
            int start = Pages.Start(StateOf(station).Page, count, Settings.PageSize);
            for (int i = 0; i < uis.Length; i++)
            {
                if (uis[i] == null)
                {
                    continue;
                }
                if (start + i < count)
                {
                    if (!uis[i].gameObject.activeSelf)
                    {
                        uis[i].gameObject.SetActive(true);
                    }
                    uis[i].SetData(factory, station, i);
                }
                else if (uis[i].gameObject.activeSelf)
                {
                    uis[i].gameObject.SetActive(false);
                }
            }
        }

        private static int CountOf(StationComponent station)
        {
            if (station.isCollector || station.isVeinCollector)
            {
                return 0;
            }
            return station.storage != null ? station.storage.Length : 0;
        }

        private static PageState StateOf(StationComponent station)
        {
            if (!states.TryGetValue(station, out PageState state))
            {
                state = new PageState();
                states.Add(station, state);
            }
            return state;
        }

        private static bool EnsureFields()
        {
            if (fieldsTried)
            {
                return storageUIsField != null && stationField != null && factoryField != null;
            }
            fieldsTried = true;
            Type type = typeof(UIControlPanelStationInspector);
            storageUIsField = AccessTools.Field(type, "storageUIs");
            stationField = AccessTools.Field(type, "station");
            factoryField = AccessTools.Field(type, "factory");
            return storageUIsField != null && stationField != null && factoryField != null;
        }

        private static bool MouseOverAny(UIControlPanelStationStorage[] uis)
        {
            for (int i = 0; i < uis.Length; i++)
            {
                if (uis[i] == null || !uis[i].gameObject.activeInHierarchy)
                {
                    continue;
                }
                RectTransform rect = uis[i].transform as RectTransform;
                if (rect != null && MouseOver(rect))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool MouseOver(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, camera);
        }

        private static void EnsureChrome(UIControlPanelStationInspector inspector, UIControlPanelStationStorage[] uis)
        {
            if (chrome != null || uis.Length == 0 || uis[0] == null)
            {
                return;
            }
            RectTransform parent = inspector.transform as RectTransform;
            if (parent == null)
            {
                return;
            }
            Text fontSource = uis[0].GetComponentInChildren<Text>(true);
            Font font = fontSource != null ? fontSource.font : null;
            GameObject root = new GameObject("station-assistant-cpanel-page", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            chrome = (RectTransform)root.transform;
            chrome.anchorMin = new Vector2(0.5f, 1f);
            chrome.anchorMax = new Vector2(0.5f, 1f);
            chrome.pivot = new Vector2(0.5f, 1f);
            chrome.anchoredPosition = new Vector2(0f, -8f);
            chrome.sizeDelta = new Vector2(140f, 24f);
            pageLabel = CreateLabel(chrome, font);
        }

        private static Text CreateLabel(RectTransform parent, Font font)
        {
            GameObject left = Arrow(parent, font, true);
            left.GetComponent<RectTransform>().anchoredPosition = new Vector2(-48f, 0f);
            GameObject right = Arrow(parent, font, false);
            right.GetComponent<RectTransform>().anchoredPosition = new Vector2(48f, 0f);
            GameObject go = new GameObject("label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(60f, 22f);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.75f, 0.88f, 1f, 0.95f);
            text.raycastTarget = false;
            return text;
        }

        private static GameObject Arrow(RectTransform parent, Font font, bool left)
        {
            GameObject go = new GameObject(left ? "prev" : "next", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(22f, 20f);
            go.GetComponent<Image>().color = new Color(0.62f, 0.84f, 1f, 0.55f);
            go.GetComponent<Button>().onClick.AddListener(() => Step(left ? -1 : 1));
            GameObject textGo = new GameObject("t", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = left ? "<" : ">";
            return go;
        }

        private static void Step(int delta)
        {
            if (lastInspector == null || !EnsureFields())
            {
                return;
            }
            StationComponent station = stationField.GetValue(lastInspector) as StationComponent;
            UIControlPanelStationStorage[] uis = storageUIsField.GetValue(lastInspector) as UIControlPanelStationStorage[];
            if (station == null || uis == null)
            {
                return;
            }
            PageState state = StateOf(station);
            int pages = Pages.Count(CountOf(station), Settings.PageSize);
            int next = state.Page + delta;
            if (next < 0 || next >= pages || next == state.Page)
            {
                return;
            }
            state.Page = next;
            Rebind(lastInspector, station, uis);
        }

        private static void Hide()
        {
            if (chrome != null && chrome.gameObject.activeSelf)
            {
                chrome.gameObject.SetActive(false);
            }
        }
    }
}
