﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StefDevs;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static GameData gameDataInstance;
    public GameData gameData;

    void Awake()
    {
        gameDataInstance = gameData;
    }

    void Start()
    {
        Methods.Main_Start(gameData);
    }

    void Update()
    {
        Methods.Main_Update(gameData);
    }
}
