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
            gameManager.gameState.pageDragStartEvent = new PageDragStartEvent
            {
                queued = true,
                mousePosition_viewport_x = Input.mousePosition.x / Screen.width,
                panelCenterPosition_viewport_x = Camera.main.WorldToViewportPoint(transform.position).x
            };
        }
    }
}
