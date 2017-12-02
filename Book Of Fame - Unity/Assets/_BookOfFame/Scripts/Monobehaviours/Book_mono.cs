using UnityEngine;
using BookOfFame;

public class Book_mono : MonoBehaviour
{
    public Book data;
    public GameManager gameManager;


    public void UIEvent_OpenBook()
    {
        gameManager.gameState.uiEvents |= UIEvents.OPEN_BOOK;
        Methods.User_Enter_Mode_Book_Viewing(gameManager.gameState.user, gameManager.gameState.book);
    }
    public void UIEvent_CloseBook()
    {
        gameManager.gameState.uiEvents |= UIEvents.CLOSE_BOOK;
        Methods.User_Enter_Mode_Locomotion(gameManager.gameState.user);
    }
    public void UIEvent_NextPage()
    {
        gameManager.gameState.uiEvents |= UIEvents.NEXT_PAGE;
        Methods.Book_TurnPage(true);
    }
    public void UIEvent_PreviousPage()
    {
        gameManager.gameState.uiEvents |= UIEvents.PREV_PAGE;
        Methods.Book_TurnPage(false);
    }

}
