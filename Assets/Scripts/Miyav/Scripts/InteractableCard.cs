using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DorkyProductions.Views;

namespace DorkyProductions
{
    public class InteractableCard : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField] private float longPressDelay = 0.5f;
        [SerializeField] private CardVisual visuals;
        public static System.Action<Guid> OnTapped;
        public static System.Action<Guid> OnLongPressed;

        public CatCardData Data { get; private set;}
        private bool _isInitialized = false;
        private float _pointerDownTime;
        private float _pointerUpTime;
        private bool _isActive = false;
        public void Initialize(CatCardData data, bool isActive)
        {
            this.Data = data; 
            _isInitialized = true;
            _isActive = isActive;
            visuals.Setup(this.Data.sprite, false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isActive) return;
            if (!_isInitialized)
                Debug.LogError($"{nameof(InteractableCard)} has not been initialized.");
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Debug.Log($"OnPointerDown on {this.gameObject.name}");
            _pointerDownTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isActive) return;
            if (!_isInitialized)
                Debug.LogError($"{nameof(InteractableCard)} has not been initialized.");
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _pointerUpTime = Time.time;
            bool isLongPress = _pointerUpTime - _pointerDownTime > longPressDelay;
            if (isLongPress)
            {
                OnLongPressed?.Invoke(Data.id);
            }
            else
            {
                OnTapped?.Invoke(Data.id);
            }
        }

        public void OnPlayedFromHand()
        {
            visuals.OnPlayedFromHand();
        }
    }
}