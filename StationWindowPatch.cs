using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StationAssistant
{
    [HarmonyPatch]
    public static class StationWindowPatch
    {
        private const int RowTop = -90;
        private const int RowHeight = 76;
        private const float ScreenMargin = 80f;

        private static readonly AccessTools.FieldRef<UIStationWindow, UIStationStorage[]> storageUIsRef =
            AccessTools.FieldRefAccess<UIStationWindow, UIStationStorage[]>("storageUIs");

        private static int page;
        private static int lastStationId;
        private static float lastWheel;
        private static float nextPageTime;
        private static UIStationWindow lastWindow;
        private static RectTransform chrome;
        private static Text pageLabel;

        internal static bool CursorOverPagingArea()
        {
            if (!Settings.StationScroll || lastWindow == null || lastWindow.stationId == 0)
            {
                return false;
            }
            if (!lastWindow.gameObject.activeInHierarchy)
            {
                return false;
            }
            return MouseOver(lastWindow.windowTrans);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        [CatAPI.Guard("a94c837d579532f7")]
        public static void OnStationIdChange_Prefix(UIStationWindow __instance)
        {
            if (!__instance.active || __instance.transport == null || __instance.stationId <= 0)
            {
                return;
            }
            StationComponent station = __instance.transport.stationPool[__instance.stationId];
            if (station == null || station.id != __instance.stationId || station.storage == null)
            {
                return;
            }
            if (!SlotStorage.IsTower(station))
            {
                return;
            }
            SlotStorage.ExpandIfNeeded(station, false);
            int rows = Settings.StationScroll ? Settings.PageSize : station.storage.Length;
            EnsureRows(__instance, rows);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        [CatAPI.Guard("a94c837d579532f7")]
        public static void OnStationIdChange_Postfix(UIStationWindow __instance)
        {
            FitToScreen(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "RefreshTrans")]
        [CatAPI.Guard("5d8291ee6f5744c2")]
        public static void RefreshTrans_Postfix(UIStationWindow __instance, StationComponent station)
        {
            if (SlotStorage.IsTower(station) && station.storage != null)
            {
                int count = station.storage.Length;
                int targetVisible = Settings.StationScroll
                    ? Mathf.Min(Settings.PageSize, count)
                    : count;
                int trim = count - targetVisible;
                if (trim != 0)
                {
                    Vector2 size = __instance.windowTrans.sizeDelta;
                    __instance.windowTrans.sizeDelta = new Vector2(size.x, size.y - RowHeight * trim);
                }
            }
            FitToScreen(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        [CatAPI.Guard("6c606ffa0548b6f2")]
        public static void OnUpdate_Postfix(UIStationWindow __instance)
        {
            ManualSlotButton.Ensure(__instance);
            if (__instance.stationId == 0 || __instance.transport == null)
            {
                SetChrome(false);
                return;
            }
            if (__instance.stationId >= __instance.transport.stationPool.Length)
            {
                return;
            }
            StationComponent station = __instance.transport.stationPool[__instance.stationId];
            if (station == null || station.id != __instance.stationId || station.storage == null)
            {
                return;
            }
            if (!SlotStorage.IsTower(station))
            {
                SetChrome(false);
                return;
            }
            UIStationStorage[] rows = storageUIsRef(__instance);
            if (rows == null || rows.Length == 0)
            {
                return;
            }
            lastWindow = __instance;
            int count = station.storage.Length;
            if (!Settings.StationScroll)
            {
                BindStorageRows(station, rows, 0, count);
                SetChrome(false);
                return;
            }
            int pageSize = Settings.PageSize;
            int pages = Pages.Count(count, pageSize);
            if (__instance.stationId != lastStationId)
            {
                lastStationId = __instance.stationId;
                page = 0;
            }
            if (pages > 1)
            {
                page += PageStep(__instance);
            }
            if (page < 0)
            {
                page = 0;
            }
            if (page >= pages)
            {
                page = pages - 1;
            }
            int start = Pages.Start(page, count, pageSize);
            int visible = Mathf.Min(pageSize, count);
            BindStorageRows(station, rows, start, visible);
            EnsureChrome(__instance);
            SetChrome(pages > 1);
            if (pages > 1 && pageLabel != null)
            {
                pageLabel.text = (page + 1) + "/" + pages;
            }
        }

        private static void BindStorageRows(StationComponent station, UIStationStorage[] rows, int start, int visible)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null)
                {
                    continue;
                }
                if (i < visible)
                {
                    int index = start + i;
                    if (!rows[i].gameObject.activeSelf)
                    {
                        rows[i].gameObject.SetActive(true);
                    }
                    if (rows[i].station != station || rows[i].index != index)
                    {
                        rows[i].station = station;
                        rows[i].index = index;
                        rows[i]._Open();
                    }
                }
                else if (rows[i].gameObject.activeSelf)
                {
                    rows[i].station = null;
                    rows[i]._Close();
                    rows[i].gameObject.SetActive(false);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnClose")]
        [CatAPI.Guard("9e2a9bfe01bb5e94")]
        public static void OnClose_Postfix()
        {
            lastWindow = null;
            SetChrome(false);
        }

        private static int PageStep(UIStationWindow window)
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                return 1;
            }
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                return -1;
            }
            float wheel = VFInput.mouseWheel;
            if (wheel == 0f)
            {
                wheel = Input.mouseScrollDelta.y;
            }
            if (wheel == 0f)
            {
                lastWheel = 0f;
                return 0;
            }
            if (wheel == lastWheel || Time.unscaledTime < nextPageTime || !MouseOver(window.windowTrans))
            {
                return 0;
            }
            lastWheel = wheel;
            nextPageTime = Time.unscaledTime + 0.15f;
            return wheel > 0f ? -1 : 1;
        }

        private static bool MouseOver(RectTransform rect)
        {
            if (rect == null)
            {
                return false;
            }
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, camera);
        }

        private static void EnsureRows(UIStationWindow window, int count)
        {
            UIStationStorage[] rows = storageUIsRef(window);
            if (rows.Length >= count)
            {
                return;
            }
            UIStationStorage[] expanded = new UIStationStorage[count];
            rows.CopyTo(expanded, 0);
            for (int i = rows.Length; i < count; i++)
            {
                UIStationStorage row = Object.Instantiate(window.storageUIPrefab, window.storageUIPrefab.transform.parent);
                ((RectTransform)row.transform).anchoredPosition = new Vector2(40f, RowTop - RowHeight * i);
                row.stationWindow = window;
                row._Create();
                if (window.inited)
                {
                    row._Init(window.data);
                }
                expanded[i] = row;
            }
            storageUIsRef(window) = expanded;
        }

        private static void FitToScreen(UIStationWindow window)
        {
            RectTransform windowTrans = window.windowTrans;
            Canvas canvas = windowTrans.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }
            float height = windowTrans.sizeDelta.y;
            float available = ((RectTransform)canvas.rootCanvas.transform).rect.height - ScreenMargin;
            float scale = height > available && height > 0f ? available / height : 1f;
            windowTrans.localScale = new Vector3(scale, scale, 1f);
        }

        private static void EnsureChrome(UIStationWindow window)
        {
            if (chrome != null)
            {
                return;
            }
            Text fontSource = window.titleText;
            Font font = fontSource != null ? fontSource.font : null;
            GameObject root = new GameObject("station-assistant-page", typeof(RectTransform));
            root.transform.SetParent(window.windowTrans, false);
            chrome = (RectTransform)root.transform;
            chrome.anchorMin = new Vector2(0.5f, 1f);
            chrome.anchorMax = new Vector2(0.5f, 1f);
            chrome.pivot = new Vector2(0.5f, 1f);
            chrome.anchoredPosition = new Vector2(28f * 2.5f, -18f);
            chrome.sizeDelta = new Vector2(160f, 28f);
            CreateArrow(chrome, font, true);
            pageLabel = CreateLabel(chrome, font);
            CreateArrow(chrome, font, false);
        }

        private static void CreateArrow(RectTransform parent, Font font, bool left)
        {
            GameObject go = new GameObject(left ? "prev" : "next", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(left ? 0f : 1f, 0.5f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(28f, 24f);
            rt.anchoredPosition = new Vector2(left ? 37f : -37f, -(rt.sizeDelta.y * 1.5f));
            go.GetComponent<Image>().color = new Color(0.62f, 0.84f, 1f, 0.55f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(() => page += left ? -1 : 1);
            GameObject textGo = new GameObject("t", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = left ? "<" : ">";
        }

        private static Text CreateLabel(RectTransform parent, Font font)
        {
            GameObject go = new GameObject("label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 36f);
            rt.anchoredPosition = new Vector2(0f, -36f);
            Text text = go.GetComponent<Text>();
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = font;
            text.fontSize = Mathf.CeilToInt(14f * 1.3f);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.75f, 0.88f, 1f, 0.95f);
            text.raycastTarget = false;
            return text;
        }

        private static void SetChrome(bool show)
        {
            if (chrome != null && chrome.gameObject.activeSelf != show)
            {
                chrome.gameObject.SetActive(show);
            }
        }
    }
}
