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
            field.ActivateInputField();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(field.gameObject);
            }
        }

        private static InputField NumberField()
        {
            UIGame game = UIRoot.instance != null ? UIRoot.instance.uiGame : null;
            if (game == null || game.dysonEditor == null || game.dysonEditor.controlPanel == null)
            {
                return null;
            }
            UIDEInspector inspector = game.dysonEditor.controlPanel.inspector;
            if (inspector == null || inspector.layerInfo == null)
            {
                return null;
            }
            return inspector.layerInfo.input0;
        }

        private static void Prepare(InputField field, InputField.ContentType content, string initial)
        {
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
            const float gap = 10f;
            window.sizeDelta += new Vector2(0f, height + gap);
            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            message.GetWorldCorners(corners);
            Vector3 bottomLocal = window.InverseTransformPoint((corners[0] + corners[3]) * 0.5f);
            inputRt.SetParent(window, false);
            inputRt.anchorMin = new Vector2(0.5f, 0.5f);
            inputRt.anchorMax = new Vector2(0.5f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 1f);
            inputRt.sizeDelta = new Vector2(140f, height);
            float anchorY = (0.5f - window.pivot.y) * window.rect.height;
            inputRt.anchoredPosition = new Vector2(0f, bottomLocal.y - anchorY - gap);
        }
    }
}
