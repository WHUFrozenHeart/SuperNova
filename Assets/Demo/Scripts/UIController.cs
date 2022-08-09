using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : SingletonController<UIController>
{
    // 功率相关
    public Text powerText;
    public Slider powerSlider;
    private int PowerValue
    {
        set
        {
            int power = Mathf.Clamp(value, 10, 100);
            powerSlider.value = (float)power / 100;
            powerText.text = power.ToString() + "%";
        }
    }
    // 质量（生命）相关
    public Slider massSlider;
    private float minMass;
    private float maxMass;
    private float CurrentMass
    {
        set
        {
            maxMass = Mathf.Max(maxMass, value);
            massSlider.value = (value - minMass) / (maxMass - minMass);
        }
    }
    // 分数相关
    public Text scoreText;
    private int CurrentScore
    {
        get
        {
            return int.Parse(scoreText.text);
        }
        set
        {
            scoreText.text = Mathf.Max(0.0f, value).ToString().PadLeft(6, '0');
        }
    }

    public void SetPower(int power)
    {
        PowerValue = power;
    }

    public void SetMass(float mass)
    {
        CurrentMass = mass;
    }

    public void InitMass(float minMass, float maxMass)
    {
        this.minMass = minMass;
        this.maxMass = maxMass;
    }

    public void AddScore(int score)
    {
        CurrentScore += score;
    }
    public void SetScore(int score)
    {
        CurrentScore = score;
    }

    public void SaveScore()
    {
        List<int> scores = new List<int>();
        for(int i = 1;i < 11;++i)
        {
            int score = PlayerPrefs.GetInt(i.ToString(), -1);
            if(score == -1)
            {
                break;
            }
            scores.Add(score);
        }
        scores.Add(CurrentScore);
        scores.Sort();
        scores.Reverse();
        for(int i = 0;i < Mathf.Min(10, scores.Count);++i)
        {
            PlayerPrefs.SetInt((i + 1).ToString(), scores[i]);
        }
    }
}
