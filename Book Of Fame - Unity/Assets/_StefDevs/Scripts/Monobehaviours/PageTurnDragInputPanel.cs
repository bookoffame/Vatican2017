using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageTurnDragInputPanel : MonoBehaviour {
    public void OnClick()
    {
        float mousePosition_viewport_x = Input.mousePosition.x / Screen.width;
        float panelCenterPosition_viewport_x = Camera.main.WorldToViewportPoint(transform.position).x;
        Methods.Start_Drag(GameManager.gameDataInstance, mousePosition_viewport_x, panelCenterPosition_viewport_x);
    }
}
