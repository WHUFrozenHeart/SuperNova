using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public List<Button> buttons;
    public float leftBorder = -72.5f;
    public float rightBorder = 72.5f;
    public float changeSpeed = 725.0f;
    public GameObject scorePanle;
    public GameObject infoPanle;
    private Button currentButton;
    private RectTransform currentTransform;
    private Button nextButton = null;
    private RectTransform nextTransform = null;
    private int index = 0;
    private bool isShift = false;
    private bool toRight = true;
    private bool isShowPanel = false;

    private void Start()
    {
        Cursor.visible = true;
        currentButton = buttons[index];
        currentTransform = currentButton.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if(isShowPanel == false)
        {
            GetShiftKey();
            UpdateButtonShift();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseScoreButtonClicked();
                CloseInfoButtonClicked();
            }
        }
    }

    private void GetShiftKey()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            LeftButtonClicked();
        }
        else if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            RightButtonClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            currentButton.onClick.Invoke();
        }
    }

    private void UpdateButtonShift()
    {
        if (isShift)
        {
            if (toRight)
            {
                currentTransform.anchoredPosition = new Vector2(Mathf.Min(rightBorder, currentTransform.anchoredPosition.x + Time.deltaTime * changeSpeed), currentTransform.anchoredPosition.y);
                nextTransform.anchoredPosition = new Vector2(Mathf.Min(0.0f, nextTransform.anchoredPosition.x + Time.deltaTime * changeSpeed), nextTransform.anchoredPosition.y);
            }
            else
            {
                currentTransform.anchoredPosition = new Vector2(Mathf.Max(leftBorder, currentTransform.anchoredPosition.x - Time.deltaTime * changeSpeed), currentTransform.anchoredPosition.y);
                nextTransform.anchoredPosition = new Vector2(Mathf.Max(0.0f, nextTransform.anchoredPosition.x - Time.deltaTime * changeSpeed), nextTransform.anchoredPosition.y);
            }
            if(nextTransform.anchoredPosition.x == 0.0f)
            {
                currentButton.enabled = false;
                currentButton.gameObject.SetActive(false);
                currentButton = nextButton;
                currentTransform = nextTransform;
                nextButton = null;
                nextTransform = null;
                isShift = false;
            }
        }
    }

    public void StartButtonClicked()
    {
        SceneManager.LoadScene("demo");
    }

    public void ScoreButtonClicked()
    {
        // 展示积分排行
        scorePanle.SetActive(true);
        isShowPanel = true;
        ScoreController.Instance.UpdateScore();
    }

    public void CloseScoreButtonClicked()
    {
        // 关闭积分排行
        scorePanle.SetActive(false);
        isShowPanel = false;
        ScoreController.Instance.ExitScorePanle();
    }

    public void InfoButtonClicked()
    {
        // 展示游戏说明的图片
        infoPanle.SetActive(true);
        isShowPanel = true;
    }

    public void CloseInfoButtonClicked()
    {
        // 游戏说明图片关闭按钮
        infoPanle.SetActive(false);
        isShowPanel = false;
    }

    public void ExitButtonClicked()
    {
        Application.Quit();
    }

    public void LeftButtonClicked()
    {
        if(isShift)
        {
            return;
        }
        index = (index + buttons.Count - 1) % buttons.Count;
        isShift = true;
        toRight = false;
        ChangeShiftState();
        nextTransform.anchoredPosition = new Vector2(rightBorder, nextTransform.anchoredPosition.y);
    }

    public void RightButtonClicked()
    {
        if(isShift)
        {
            return;
        }
        index = (index + 1) % buttons.Count;
        isShift = true;
        toRight = true;
        ChangeShiftState();
        nextTransform.anchoredPosition = new Vector2(leftBorder, nextTransform.anchoredPosition.y);
    }

    private void ChangeShiftState()
    {
        nextButton = buttons[index];
        nextButton.enabled = true;
        nextButton.gameObject.SetActive(true);
        nextTransform = nextButton.GetComponent<RectTransform>();
    }
}
