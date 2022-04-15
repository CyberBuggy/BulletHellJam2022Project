using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

namespace CyberBuggy.MenuScreenUtils
{
    public class ButtonColorSwitcher : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _targetButton;
        [SerializeField] private TMP_Text _buttonText;
        [SerializeField] private Color _primaryColor = new Color(255, 255, 255, 255);
        [SerializeField] private Color _secondaryColor;

        [SerializeField] private ButtonHighlightType _buttonHighlightType = ButtonHighlightType.BySelect;

        [Flags]
        private enum ButtonHighlightType
        {
            BySelect = 1,
            ByPointerEnter = 2
        }


        public void OnSelect(BaseEventData eventData)
        {
            if(_buttonHighlightType == ButtonHighlightType.BySelect)
                ApplyButtonColors(_secondaryColor, _primaryColor);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if(_buttonHighlightType == ButtonHighlightType.BySelect)
                ApplyButtonColors(_primaryColor, _secondaryColor);
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if(_buttonHighlightType == ButtonHighlightType.ByPointerEnter)
                ApplyButtonColors(_secondaryColor, _primaryColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(_buttonHighlightType == ButtonHighlightType.ByPointerEnter)
                ApplyButtonColors(_primaryColor, _secondaryColor);
        }
        private void OnDisable()
        {
            ApplyButtonColors(_primaryColor, _secondaryColor);
        }
        private void ApplyButtonColors(Color firstColor, Color secondColor)
        {
            _targetButton.targetGraphic.color = firstColor;
            _buttonText.color = secondColor;
        }

        
    }
}
