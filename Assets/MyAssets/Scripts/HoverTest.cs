using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTest : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("HoverTest: pointer entered");
    }
}