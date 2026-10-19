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
            bool tower = false;
            int slotCount = 0;
            if (window.transport != null && window.stationId > 0 && window.stationId < window.transport.stationPool.Length)
            {
                StationComponent current = window.transport.stationPool[window.stationId];
                tower = SlotStorage.IsTower(current);
                slotCount = tower ? current.storage.Length : 0;
            }
            if (!Settings.ManualSlotButton || !tower)
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
                ApplyLayout((RectTransform)existing, window);
                Text label = existing.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = SlotButtonText(slotCount);
                    label.color = Color.white;
                }
                return;
            }
            UIButton sample = window.stationSettingTabBtn;
            Image sampleImage = sample != null ? sample.GetComponent<Image>() : null;
            Text sampleText = sample != null ? sample.GetComponentInChildren<Text>(true) : null;
            Font font = sampleText != null ? sampleText.font : (window.titleText != null ? window.titleText.font : null);
            GameObject go = new GameObject("station-assistant-set-slots", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(window.windowTrans, false);
            Image image = go.GetComponent<Image>();
            if (sampleImage != null && sampleImage.sprite != null)
            {
                image.sprite = sampleImage.sprite;
                image.type = sampleImage.type;
                image.color = sampleImage.color;
            }
            else
            {
                image.color = new Color(0.16f, 0.28f, 0.38f, 0.9f);
            }
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
            text.fontSize = sampleText != null ? sampleText.fontSize : 13;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = SlotButtonText(slotCount);
            ApplyLayout((RectTransform)go.transform, window);
        }

        private static void ApplyLayout(RectTransform rt, UIStationWindow window)
        {
            RectTransform sample = window.stationSettingTabBtn != null
                ? window.stationSettingTabBtn.transform as RectTransform
                : null;
            float sampleWidth = sample != null && sample.rect.width > 1f ? sample.rect.width : 132f;
            float sampleHeight = sample != null && sample.rect.height > 1f ? sample.rect.height : 26f;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(sampleWidth, sampleHeight * 1.3f);
            rt.anchoredPosition = new Vector2(-16f, -(sampleHeight + 18f));
        }

        private static string SlotButtonText(int count)
        {
            return CatAPI.Lang.En("Slots: " + count).ZhCN("槽位数目 : " + count);
        }

        private static string DialogTitle()
        {
            return CatAPI.Lang.En("Set slot count").ZhCN("设置槽位数目");
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
            PopNumber.Show(DialogTitle(), current.ToString(), CatAPI.Lang.En($"Item kinds this station can hold. Current: {current}").ZhCN($"物流塔可容纳的物品数种类，目前为{current}"), InputField.ContentType.IntegerNumber, text =>
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
