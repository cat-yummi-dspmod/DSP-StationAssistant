using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StationAssistant
{
    internal static class PopNumber
    {
        internal static void Show(string title, string initial, string message, InputField.ContentType content, Action<string> onConfirm)
        {
            InputField source = NumberField();
            if (source == null)
            {
                return;
            }
            string cancel = Localization.Translate("Cancel");
            string ok = Localization.Translate("OK");
            InputField field = null;
            UIMessageBox box = UIMessageBox.Show(title, message, cancel, ok, UIMessageBox.QUESTION, null, () =>
            {
                if (field != null)
                {
                    onConfirm?.Invoke(field.text);
                }
            });
            if (box == null || box.m_WindowTrans == null || box.m_MessageText == null)
            {
                return;
            }
            field = UnityEngine.Object.Instantiate(source, box.m_WindowTrans);
            field.gameObject.name = "pop-number";
            Prepare(field, content, initial);
            RectTransform inputRt = (RectTransform)field.transform;
            float height = SourceHeight(source);
            ResetRect(inputRt, height);
            PlaceBetweenMessageAndButtons(box, inputRt, height);
            EscClose esc = field.gameObject.AddComponent<EscClose>();
            esc.box = box;
            esc.field = field;
            field.ActivateInputField();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(field.gameObject);
            }
        }

        private static InputField NumberField()
        {
            UIGame game = UIRoot.instance != null ? UIRoot.instance.uiGame : null;
            if (game != null && game.dysonEditor != null && game.dysonEditor.controlPanel != null)
            {
                UIDEInspector inspector = game.dysonEditor.controlPanel.inspector;
                if (inspector != null && inspector.layerInfo != null && inspector.layerInfo.input0 != null)
                {
                    return inspector.layerInfo.input0;
                }
            }
            // 重新进入游戏后戴森球编辑器可能尚未就绪：退而求其次，用物流站面板的名称输入框作样板。
            if (game != null && game.stationWindow != null && game.stationWindow.nameInput != null)
            {
                return game.stationWindow.nameInput;
            }
            Plugin.Log?.LogWarning("PopNumber：找不到可复制的输入框样板，双击编辑无法弹出。");
            return null;
        }

        private static void Prepare(InputField field, InputField.ContentType content, string initial)
        {
            UIInputField uiInput = field.GetComponent<UIInputField>();
            if (uiInput != null)
            {
                UnityEngine.Object.DestroyImmediate(uiInput);
            }
            UIButton button = field.GetComponent<UIButton>();
            if (button != null)
            {
                UnityEngine.Object.DestroyImmediate(button);
            }
            field.onValueChanged = new InputField.OnChangeEvent();
            field.onEndEdit = new InputField.EndEditEvent();
            field.interactable = true;
            field.characterLimit = 0;
            bool integer = content != InputField.ContentType.DecimalNumber;
            field.contentType = integer ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber;
            field.characterValidation = integer ? InputField.CharacterValidation.Integer : InputField.CharacterValidation.Decimal;
            field.text = initial ?? "";
            if (field.placeholder != null)
            {
                field.placeholder.enabled = false;
            }
            if (field.textComponent != null)
            {
                field.textComponent.fontStyle = FontStyle.Normal;
                field.textComponent.alignment = TextAnchor.MiddleCenter;
                field.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
                RectTransform textRt = field.textComponent.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.offsetMin = new Vector2(4f, 1f);
                textRt.offsetMax = new Vector2(-4f, -1f);
            }
        }

        private static float SourceHeight(InputField source)
        {
            float height = ((RectTransform)source.transform).rect.height;
            if (height < 16f || height > 32f)
            {
                return 20f;
            }
            return height;
        }

        private static void ResetRect(RectTransform rt, float height)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(140f, height);
        }

        private static void PlaceBetweenMessageAndButtons(UIMessageBox box, RectTransform inputRt, float height)
        {
            RectTransform window = box.m_WindowTrans;
            RectTransform message = box.m_MessageText.rectTransform;
            const float gap = 8f;
            window.sizeDelta += new Vector2(0f, height + gap);
            Canvas.ForceUpdateCanvases();
            Text text = box.m_MessageText;
            // 用文字实际渲染出的最底部顶点定位（多行、字体、缩放都自然适配），而不是猜文本框高度。
            Vector3 bottomLocal;
            if (!TryGlyphBottom(text, window, out bottomLocal))
            {
                Vector3[] corners = new Vector3[4];
                message.GetWorldCorners(corners);
                bottomLocal = window.InverseTransformPoint((corners[0] + corners[3]) * 0.5f);
            }
            inputRt.SetParent(window, false);
            inputRt.anchorMin = new Vector2(0.5f, 0.5f);
            inputRt.anchorMax = new Vector2(0.5f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 1f);
            inputRt.sizeDelta = new Vector2(140f, height);
            Place(inputRt, window, bottomLocal, gap);
            // 弹窗出现后文字可能还会被重新排版（字体图集、缩放动画），之后几帧持续贴住文字底边。
            Follow follow = inputRt.gameObject.AddComponent<Follow>();
            follow.text = text;
            follow.window = window;
            follow.gap = gap;
        }

        private static void Place(RectTransform inputRt, RectTransform window, Vector3 bottomLocal, float gap)
        {
            float anchorY = (0.5f - window.pivot.y) * window.rect.height;
            inputRt.anchoredPosition = new Vector2(0f, bottomLocal.y - anchorY - gap);
        }

        /// <summary>输入框聚焦时 Esc 只会让输入框失焦，游戏不会收到，导致要按两次。这里直接当作「取消」。</summary>
        private sealed class EscClose : MonoBehaviour
        {
            internal UIMessageBox box;
            internal InputField field;
            private bool wasFocused;

            private void Update()
            {
                if (box == null || field == null)
                {
                    enabled = false;
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Escape) && (wasFocused || field.isFocused))
                {
                    enabled = false;
                    box.OnButton1Click();
                    return;
                }
                wasFocused = field.isFocused;
            }
        }

        private sealed class Follow : MonoBehaviour
        {
            internal Text text;
            internal RectTransform window;
            internal float gap;
            private int frames;

            private void LateUpdate()
            {
                if (text == null || window == null || frames++ > 30)
                {
                    enabled = false;
                    return;
                }
                if (TryGlyphBottom(text, window, out Vector3 bottomLocal))
                {
                    Place((RectTransform)transform, window, bottomLocal, gap);
                }
            }
        }

        private static bool TryGlyphBottom(Text text, RectTransform window, out Vector3 bottomLocal)
        {
            bottomLocal = Vector3.zero;
            if (text == null)
            {
                return false;
            }
            System.Collections.Generic.IList<UIVertex> verts = text.cachedTextGenerator.verts;
            if (verts == null || verts.Count == 0)
            {
                text.Rebuild(CanvasUpdate.PreRender);
                verts = text.cachedTextGenerator.verts;
            }
            if (verts == null || verts.Count == 0)
            {
                return false;
            }
            float minY = float.MaxValue;
            for (int i = 0; i < verts.Count; i++)
            {
                if (verts[i].position.y < minY)
                {
                    minY = verts[i].position.y;
                }
            }
            float unit = text.pixelsPerUnit > 0f ? 1f / text.pixelsPerUnit : 1f;
            Vector3 world = text.rectTransform.TransformPoint(new Vector3(0f, minY * unit, 0f));
            bottomLocal = window.InverseTransformPoint(world);
            return true;
        }
    }
}
