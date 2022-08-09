using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseGamePanelController : SingletonController<PauseGamePanelController>
{
    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowPanel()
    {
        Cursor.visible = true;
        gameObject.SetActive(true);
    }

    public void RestartButtonClicked()
    {
        ContinueButtonClicked();
        UIController.Instance.SaveScore();
        SceneManager.LoadScene("demo");
    }

    public void BackButtonClicked()
    {
        ContinueButtonClicked();
        UIController.Instance.SaveScore();
        SceneManager.LoadScene("MainMenu");
    }

    public void ContinueButtonClicked()
    {
        Cursor.visible = false;
        gameObject.SetActive(false);
        GameController.Instance.ResumeGame();
    }
}
