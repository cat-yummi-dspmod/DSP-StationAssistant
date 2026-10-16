using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StationAssistant
{
    internal static class NumericEditDialog
    {
        private static Font font;

        internal static void Show(string title, string initial, string hint, InputField.ContentType content, Action<string> onConfirm)
        {
            if (UIRoot.instance == null)
            {
                return;
            }
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            Transform parent = UIRoot.instance.uiGame != null ? UIRoot.instance.uiGame.transform : UIRoot.instance.transform;
            GameObject overlay = new GameObject("station-assistant-edit");
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsLastSibling();
            RectTransform full = overlay.AddComponent<RectTransform>();
            full.anchorMin = Vector2.zero;
            full.anchorMax = Vector2.one;
            full.offsetMin = Vector2.zero;
            full.offsetMax = Vector2.zero;
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            Action close = () => UnityEngine.Object.Destroy(overlay);
            EventTrigger trigger = overlay.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => close());
            trigger.triggers.Add(entry);

            GameObject panel = new GameObject("panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.sizeDelta = new Vector2(420f, 180f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

            Text titleText = Label(panel.transform, font, title, new Vector2(0f, -16f), 16);
            titleText.alignment = TextAnchor.MiddleCenter;
            Text hintText = Label(panel.transform, font, hint, new Vector2(0f, -48f), 12);
            hintText.color = new Color(0.75f, 0.8f, 0.85f);

            GameObject inputGo = new GameObject("input", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(panel.transform, false);
            RectTransform inputRt = (RectTransform)inputGo.transform;
            inputRt.anchoredPosition = new Vector2(0f, -8f);
            inputRt.sizeDelta = new Vector2(360f, 32f);
            inputGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);
            GameObject inputTextGo = new GameObject("text", typeof(RectTransform), typeof(Text));
            inputTextGo.transform.SetParent(inputGo.transform, false);
            RectTransform inputTextRt = (RectTransform)inputTextGo.transform;
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(8f, 4f);
            inputTextRt.offsetMax = new Vector2(-8f, -4f);
            Text inputText = inputTextGo.GetComponent<Text>();
            inputText.font = font;
            inputText.fontSize = 14;
            inputText.color = Color.white;
            inputText.supportRichText = false;
            InputField field = inputGo.GetComponent<InputField>();
            field.textComponent = inputText;
            field.contentType = content;
            field.text = initial;

            Button(panel.transform, font, "确定", new Vector2(-70f, -64f), () =>
            {
                onConfirm?.Invoke(field.text);
                close();
            });
            Button(panel.transform, font, "取消", new Vector2(70f, -64f), close);
        }

        private static Text Label(Transform parent, Font labelFont, string value, Vector2 pos, int size)
        {
            GameObject go = new GameObject("label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-24f, 24f);
            Text text = go.GetComponent<Text>();
            text.font = labelFont;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static void Button(Transform parent, Font labelFont, string value, Vector2 pos, Action action)
        {
            GameObject go = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(100f, 28f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.32f, 0.42f, 1f);
            go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => action());
            Text text = Label(go.transform, labelFont, value, new Vector2(0f, -2f), 14);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
