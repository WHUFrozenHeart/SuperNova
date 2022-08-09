using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverPanelController : SingletonController<GameOverPanelController>
{
    public Text deathReasonText;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowPanel(string reason)
    {
        Cursor.visible = true;
        gameObject.SetActive(true);
        deathReasonText.text = reason;
    }

    public void RestartButtonClicked()
    {
        SceneManager.LoadScene("demo");
    }

    public void BackButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
