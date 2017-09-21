using UnityEngine;

public class Book_mono : MonoBehaviour
{
    public Book data;


    public void UIEvent_OpenBook()
    {
        Methods.User_Enter_Mode_Book_Viewing(GameManager.gameDataInstance.user, GameManager.gameDataInstance.book);
    }
    public void UIEvent_CloseBook()
    {
        Methods.User_Enter_Mode_Locomotion(GameManager.gameDataInstance.user);
    }
    public void UIEvent_NextPage()
    {
        Methods.Book_TurnPage(true);
    }
    public void UIEvent_PreviousPage()
    {
        Methods.Book_TurnPage(false);
    }

}
