using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class DialogTextManager : MonoBehaviour
{
    [SerializeField] private UnityEvent onCompletedEvents = new UnityEvent();
    [SerializeField] float eventDelayTime;    

    public UnityAction onClickText;
    public string[] scenarios;
    [SerializeField] Text uiText;

    [SerializeField]
    [Range(0.001f, 0.3f)]
    float intervalForCharacterDisplay = 0.1f;

    private string currentText = string.Empty;
    private float timeUntilDisplay = 0;
    private float timeElapsed = 1;
    private int currentLine = 0;
    private int lastUpdateCharacter = -1;

    public static DialogTextManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    
    public bool IsCompleteDisplayText
    {
        get { return Time.time > timeElapsed + timeUntilDisplay; }
    }

    void Update()
    {
        if (IsCompleteDisplayText)
        {
            if (currentLine < scenarios.Length && Input.GetMouseButtonDown(0))
            {
                SetNextLine();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                timeUntilDisplay = 0;
            }
        }

        int displayCharacterCount;

        // クリックによる全文表示後は表示文字数を固定する
        if (timeUntilDisplay <= 0f)
        {
            displayCharacterCount = currentText.Length;
        }
        else
        {
            displayCharacterCount =
                (int)(Mathf.Clamp01((Time.time - timeElapsed) / timeUntilDisplay)
                * currentText.Length);
        }

        displayCharacterCount = Mathf.Clamp(displayCharacterCount, 0, currentText.Length);

        // 表示文字数が変わったときだけUIを更新する
        if (displayCharacterCount != lastUpdateCharacter)
        {
            uiText.text = currentText.Substring(0, displayCharacterCount);
            lastUpdateCharacter = displayCharacterCount;
        }
    }

    public void SetNextLine()
    {
        if (scenarios.Length - 1 < currentLine)
        {
            return;
        }

        currentText = scenarios[currentLine];
        timeUntilDisplay = currentText.Length * intervalForCharacterDisplay;
        timeElapsed = Time.time;
        currentLine++;
        lastUpdateCharacter = -1;
    }

    public void SetScenarios(string[] sc)
    {
        scenarios = sc;
        currentLine = 0;
        SetNextLine();
    }

    public IEnumerator ShowAndWait(
        string message,
        float waitSeconds
    )
    {
        SetScenarios(new string[]
        {
            message
        });

        yield return new WaitUntil(
            () => IsCompleteDisplayText
        );

        yield return new WaitForSeconds(waitSeconds);
    }
}
