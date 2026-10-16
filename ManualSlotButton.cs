using UnityEngine;
using UnityEngine.UI;

namespace StationAssistant
{
    internal static class ManualSlotButton
    {
        internal static void Ensure(UIStationWindow window)
        {
            if (window == null || window.windowTrans == null)
            {
                return;
            }
            Transform existing = window.windowTrans.Find("station-assistant-set-slots");
            if (!Settings.ManualSlotButton)
            {
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                }
                return;
            }
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }
            Font font = window.titleText != null ? window.titleText.font : null;
            GameObject go = new GameObject("station-assistant-set-slots", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(window.windowTrans, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -12f);
            rt.sizeDelta = new Vector2(132f, 26f);
            go.GetComponent<Image>().color = new Color(0.16f, 0.28f, 0.38f, 0.9f);
            UIStationWindow captured = window;
            go.GetComponent<Button>().onClick.AddListener(() => Open(captured));
            GameObject textGo = new GameObject("t", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 13;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = "设置槽位数目";
        }

        private static void Open(UIStationWindow window)
        {
            if (window.transport == null || window.stationId <= 0)
            {
                return;
            }
            StationComponent station = window.transport.stationPool[window.stationId];
            if (!SlotStorage.IsTower(station))
            {
                return;
            }
            int current = station.storage.Length;
            NumericEditDialog.Show("设置槽位数目", current.ToString(), "1 到 32。减少时先清空被去掉的格子。", InputField.ContentType.IntegerNumber, text =>
            {
                if (!int.TryParse(text.Trim(), out int count))
                {
                    return;
                }
                SlotStorage.SetCount(window.factory, station, count);
            });
        }
    }
}
