using UnityEngine;
using BookOfFame;

public class GameManager : MonoBehaviour
{
    public GameState gameState;
    public GameParams_so gameParams;
    public SceneReferences sceneReferences;

    void Awake()
    {
        gameDataInstance = gameState;
    }

    void Start()
    {
        sceneReferences.book_mono.gameManager = this;
        sceneReferences.agentObject.gameManager = this;
        sceneReferences.lookingGlass_mono.gameManager = this;

        Methods.Main_Initialize(ref gameState, gameParams.data, sceneReferences);
    }

    void Update()
    {
        Methods.Main_Update(ref gameState, gameParams.data);
    }

    private void FixedUpdate()
    {
        Methods.Main_FixedUpdate(ref gameState, gameParams.data);
    }
}
