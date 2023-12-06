using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public TextAsset mathJson;
    public TextAsset quizJson;  // Drag your JSON file into this field in the Unity Inspector
    public TextMeshProUGUI questionText;
    public GameObject options;
    public Transform parent;
    float timer = 20;
    bool isTimer = false;
    FractionQuiz quiz;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI WinnerText;
    int score = 0;
    public GameObject winningScreen;
    private void Start()
    {
        isTimer = false;
        GetQuestions();
    }
    void GetQuestions()
    {
        if (GameData.isQuiz)
        {
            if (quizJson != null)
            {
                quiz = JsonConvert.DeserializeObject<FractionQuiz>(quizJson.ToString());
            }
            foreach (GameData question in quiz.questions)
            {
                Debug.Log("Question: " + question.question);
                Debug.Log("Options: " + string.Join(", ", question.options.ToArray()));
                Debug.Log("Answer: " + question.answer);
                Debug.Log("-------------");
            }
            DisplayQuestion();
        }
        else
        {
            if (mathJson != null)
            {
                quiz = JsonConvert.DeserializeObject<FractionQuiz>(mathJson.ToString());
            }
            foreach (GameData question in quiz.questions)
            {
                Debug.Log("Question: " + question.question);
                Debug.Log("Options: " + string.Join(", ", question.options.ToArray()));
                Debug.Log("Answer: " + question.answer);
                Debug.Log("-------------");
            }
            DisplayQuestion();
        }
       
        }

    private void Update()
    {
            if (isTimer)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    isTimer = false;
                    currentQuestionIndex++;
                if (currentQuestionIndex < quiz.questions.Count)
                {
                    DisplayQuestion();
                }
                else
                {
                    if (!isWinner)
                    {
                        isWinner = true;
                        WinnerText.text = "Total Score : " + score;
                        winningScreen.SetActive(true);
                    }
                }
                }
                else
                {
                    int minutes = Mathf.FloorToInt(timer / 60f);
                    int seconds = Mathf.RoundToInt(timer % 60f);
                    if (seconds == 60)
                    {
                        seconds = 0;
                        minutes += 1;
                    }
                    timerText.text = minutes.ToString("00") +":"+ seconds.ToString("00");
                  
                }
            }
    }
    public Image images;
    int currentQuestionIndex = 0;
    void DisplayQuestion()
    {
        isTimer = true;
        timer = 20;
        questionText.text = quiz.questions[currentQuestionIndex].question;
        string answer = quiz.questions[currentQuestionIndex].answer;
        string url =  quiz.questions[currentQuestionIndex].imageUrl;
        if (!string.IsNullOrEmpty(url))
        {
            images.enabled = true;
            StartCoroutine(LoadImage(url));
        }
            else
                    images.enabled = false;
        foreach (Transform tr in parent)
        {
            Destroy(tr.gameObject);
        }
        for (int i = 0; i < quiz.questions[currentQuestionIndex].options.Count; i++)
        {
            Button optionButton = Instantiate(options).GetComponent<Button>();
            optionButton.transform.SetParent(parent, false);
            optionButton.transform.GetChild(0).GetComponent<Text>().text = quiz.questions[currentQuestionIndex].options[i];
            optionButton.name = quiz.questions[currentQuestionIndex].options[i];
            optionButton.onClick.AddListener(() => CheckAnswer(optionButton.name, answer, optionButton));

        }
    }
    IEnumerator LoadImage(string imageUrl)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"Failed to load image: {www.error}");
            }
            else
            {
                // Convert the downloaded texture into a Sprite
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                // Assign the Sprite to the SpriteRenderer
                images.sprite = sprite;
            }
        }
    }

private void CheckAnswer(string selectedAnswer, string correctAnswer, Button bt)
    {
            if (selectedAnswer == correctAnswer)
            {
                Debug.Log("Correct answer!");
                bt.image.color = Color.green;
                score += 1;
                ScoreText.text = "Score : " + score;
            }
            else
            {
                bt.image.color = Color.red;
                Debug.Log("Wrong answer.");
            }
        StopCoroutine("SetNewQuestion");
        StartCoroutine("SetNewQuestion");
    }
    bool isWinner = false;
    IEnumerator SetNewQuestion()
    {
        yield return new WaitForSeconds(0.5f);
        currentQuestionIndex++;
        //bt.image.color = Color.white;
        // Move to the next question.
        if (currentQuestionIndex < quiz.questions.Count)
        {
            DisplayQuestion();
        }
        else
        {
            currentQuestionIndex = 0;
            timer = 20;
            isTimer = false;
            if (!isWinner)
            {
                isWinner = true;
                WinnerText.text = "Total Score : " + score;
                winningScreen.SetActive(true);
            }
            //UIManager.Instance.SetAcitveObject(UIManager.Instance.WinningPopupPanel);
        }
    }
    public void goToHome()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}

