using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button, IPointerDownHandler
{
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        onPointerDown?.Invoke();
    }

    public event Action onPointerDown;
}