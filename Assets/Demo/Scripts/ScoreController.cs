using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreController : SingletonController<ScoreController>
{
    public Text rankText;
    public Text scoreText;
    public float topBorder = 610.0f;
    public float heightGap = 50.0f;
    // 退出时销毁
    private List<Text> texts = new List<Text>();

    public void UpdateScore()
    {
        for(int i = 1;i < 11;++i)
        {
            rankText.text = "第" + i + "名";
            int score = PlayerPrefs.GetInt(i.ToString(), -1);
            if(score == -1)
            {
                scoreText.text = "暂无";
            }
            else
            {
                scoreText.text = score.ToString() + "分";
            }
            Text currentRank = Instantiate(rankText, transform);
            currentRank.gameObject.SetActive(true);
            RectTransform rankTransform = currentRank.GetComponent<RectTransform>();
            rankTransform.anchoredPosition = new Vector2(rankTransform.anchoredPosition.x, topBorder - heightGap * i);
            Text currentScore = Instantiate(scoreText, transform);
            currentScore.gameObject.SetActive(true);
            RectTransform scoreTransform = currentScore.GetComponent<RectTransform>();
            scoreTransform.anchoredPosition = new Vector2(scoreTransform.anchoredPosition.x, topBorder - heightGap * i);
            texts.Add(currentRank);
            texts.Add(currentScore);
        }
    }

    public void ExitScorePanle()
    {
        texts.ForEach(text => Destroy(text.gameObject));
        texts.Clear();
    }
}
