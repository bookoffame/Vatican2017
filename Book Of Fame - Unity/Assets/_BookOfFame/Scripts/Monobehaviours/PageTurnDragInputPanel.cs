using UnityEngine;

namespace BookOfFame
{
    public class PageTurnDragInputPanel : MonoBehaviour
    {
        public GameManager gameManager;

        private void Awake()
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        public void OnClick()
        {
            float mousePosition_viewport_x = Input.mousePosition.x / Screen.width;
            float panelCenterPosition_viewport_x = Camera.main.WorldToViewportPoint(transform.position).x;
            Methods.Start_Drag(gameManager.gameState, mousePosition_viewport_x, panelCenterPosition_viewport_x);
        }
    }
}
