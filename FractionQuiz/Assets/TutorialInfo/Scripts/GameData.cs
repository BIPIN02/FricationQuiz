using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class GameData 
{
    public static bool isQuiz = false;
    public string question;
    public List<string> options;
    public string answer;
    public string imageUrl;
}
[System.Serializable]
public class FractionQuiz
{
    public List<GameData> questions;
}

