using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZoneOpponent : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData) {}
    public void OnPointerExit(PointerEventData eventData) {}

    public void OnDrop(PointerEventData eventData)
    {}
}