using UnityEngine;
using BookOfFame;

public class Book_mono : MonoBehaviour
{
    public Book data;
    public GameManager gameManager;


    public void UIEvent_OpenBook()
    {
        gameManager.gameState.uiEvents |= UIEvents.OPEN_BOOK;
    }
    public void UIEvent_CloseBook()
    {
        gameManager.gameState.uiEvents |= UIEvents.CLOSE_BOOK;
    }
    public void UIEvent_NextPage()
    {
        gameManager.gameState.uiEvents |= UIEvents.NEXT_PAGE;
    }
    public void UIEvent_PreviousPage()
    {
        gameManager.gameState.uiEvents |= UIEvents.PREV_PAGE;
    }

}
