using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MenuController : MonoBehaviour
{
    public static MenuController instance;
    public GameObject loadingPopupPannel;
    public GameObject selectionPannel;
    public TextMeshProUGUI userName;
    public GameObject loginScreenPanel;
    public GameObject registercreenPanel;
    public GameObject verfiyScreenPanel;

    private void Awake()
    {
        instance = this;
    }
    public Canvas canvas;

    public void SetAcitveObject(GameObject go)
    {
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            canvas.transform.GetChild(i).gameObject.SetActive(false);
        }
        go.SetActive(true);
    }
    public void QuizSelection(int index)
    {
        if (index == 0)
            GameData.isQuiz = false;
        else
            GameData.isQuiz = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}
