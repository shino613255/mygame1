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
    private int lastAdvanceFrame = -1;
    private bool isAdvancePointerHeld;
    private int lastAdvanceReleaseFrame = -1;
    private int displayVersion;

    public bool ConsumedClickThisFrame =>
        lastAdvanceFrame == Time.frameCount ||
        lastAdvanceReleaseFrame == Time.frameCount ||
        (isAdvancePointerHeld && !Input.GetMouseButtonDown(0));
    public bool HasPendingLines => scenarios != null && currentLine < scenarios.Length;

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
        if (Input.GetMouseButtonDown(0))
            isAdvancePointerHeld = false;

        if (isAdvancePointerHeld && !Input.GetMouseButton(0))
        {
            lastAdvanceReleaseFrame = Time.frameCount;
            isAdvancePointerHeld = false;
        }

        if (IsCompleteDisplayText)
        {
            if (scenarios != null && currentLine < scenarios.Length && Input.GetMouseButtonDown(0))
            {
                lastAdvanceFrame = Time.frameCount;
                isAdvancePointerHeld = true;
                SetNextLine();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastAdvanceFrame = Time.frameCount;
                isAdvancePointerHeld = true;
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
        displayVersion++;
        scenarios = sc;
        currentLine = 0;
        SetNextLine();
    }

    public IEnumerator ShowAndWait(
        string message,
        float waitSeconds
    )
    {
        return ShowAndWait(new[] { message }, waitSeconds);
    }

    public IEnumerator ShowAndWait(string[] messages, float waitSeconds)
    {
        int version = displayVersion;

        foreach (string message in messages)
        {
            if (version != displayVersion)
                yield break;

            SetScenarios(new[] { message });
            version = displayVersion;

            yield return new WaitUntil(
                () => version != displayVersion || IsCompleteDisplayText
            );

            if (version != displayVersion)
                yield break;

            yield return new WaitForSeconds(waitSeconds);
        }
    }
}
