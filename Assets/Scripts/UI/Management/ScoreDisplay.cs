using TMPro;
using UnityEngine;
using System.Collections;
using Assets.Scripts.Player;

public class ScoreDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text scoreLabelText;
    [SerializeField] private TMP_Text scoreValueText;

    [SerializeField] private TMP_Text comboLabelText;
    [SerializeField] private TMP_Text comboValueText;

    [SerializeField] private TMP_Text lastHitText;

    [Header("Animation Settings")]
    [SerializeField] private Color defaultComboColor = Color.white;
    [SerializeField] private Color boostComboColor = Color.yellow;
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchTime = 0.25f;

    private RewardSystem rewardSystem;

    private int lastScore = 0;
    private int lastGoodCombo = 0;
    private int lastPerfectCombo = 0;
    private bool hasHitOccurred = false;

    [SerializeField] private Color goodComboColor = Color.cyan;
    [SerializeField] private Color perfectComboColor = Color.green;
    [SerializeField] private Color warningPulseColor = Color.red;

    void Start()
    {
        rewardSystem = FindFirstObjectByType<RewardSystem>();
        if (rewardSystem == null)
            Debug.LogError("⚠️ RewardSystem not found!");

        // Initial UI state
        scoreLabelText.text = "Score";
        comboLabelText.text = "Combo Count";
        scoreValueText.text = "0";
        comboValueText.text = "";
        lastHitText.alpha = 0;
    }

    void Update()
    {
        if (rewardSystem == null) return;

        if (rewardSystem.IsComboAboutToExpire)
        {
            float pulse = Mathf.PingPong(Time.time * 4f, 1f);
            comboValueText.color = Color.Lerp(comboValueText.color, warningPulseColor, pulse);
        }
    }

    public void UpdateUI(Attackable.HitResult hitResult)
    {
        UpdateScore();
        UpdateCombo();
        UpdateLastHit(hitResult.ToString(), hitResult);
    }

    public void UpdateUI(InteractTiming timing)
    {
        UpdateScore();
        UpdateCombo();
        UpdateLastHit(timing.ToString(), timing);
    }

    private void UpdateScore()
    {
        int currentScore = rewardSystem.TotalScore;
        if (currentScore > lastScore)
        {
            scoreValueText.text = currentScore.ToString();
            StartCoroutine(AnimateTextPunch(scoreValueText));
            lastScore = currentScore;
        }
    }

    private void UpdateCombo()
    {
        int currentCombo = Mathf.Max(rewardSystem.GoodComboCount, rewardSystem.PerfectComboCount);
        bool isPerfect = rewardSystem.PerfectComboCount > 0;

        if (currentCombo > 0)
        {
            comboValueText.text = $"{currentCombo}x";
            comboValueText.color = isPerfect ? perfectComboColor : goodComboColor;
            comboLabelText.color = comboValueText.color;

            if (currentCombo > Mathf.Max(lastGoodCombo, lastPerfectCombo))
                StartCoroutine(AnimateTextPunch(comboValueText));
        }
        else
        {
            comboValueText.text = "";
            comboValueText.color = defaultComboColor;
            comboLabelText.color = defaultComboColor;
        }

        lastGoodCombo = rewardSystem.GoodComboCount;
        lastPerfectCombo = rewardSystem.PerfectComboCount;
    }

    private void UpdateLastHit(string label, object resultType)
    {
        if (!hasHitOccurred)
        {
            lastHitText.alpha = 1;
            hasHitOccurred = true;
        }

        lastHitText.text = label;

        if (resultType is Attackable.HitResult hit)
        {
            switch (hit)
            {
                case Attackable.HitResult.Perfect:
                    lastHitText.color = perfectComboColor;
                    break;
                case Attackable.HitResult.Good:
                    lastHitText.color = goodComboColor;
                    break;
                case Attackable.HitResult.Late:
                    lastHitText.color = warningPulseColor;
                    break;
                default:
                    lastHitText.color = defaultComboColor;
                    break;
            }
        }
        else if (resultType is InteractTiming timing)
        {
            switch (timing)
            {
                case InteractTiming.Perfect:
                    lastHitText.color = perfectComboColor;
                    break;
                case InteractTiming.Good:
                    lastHitText.color = goodComboColor;
                    break;
                case InteractTiming.Late:
                    lastHitText.color = warningPulseColor;
                    break;
                default:
                    lastHitText.color = defaultComboColor;
                    break;
            }
        }
    }

    private IEnumerator AnimateTextPunch(TMP_Text text)
    {
        Color originalColor = text.color;
        Vector3 originalScale = text.transform.localScale;

        text.color = boostComboColor;
        text.transform.localScale = originalScale * punchScale;

        float t = 0;
        while (t < punchTime)
        {
            t += Time.deltaTime;
            text.transform.localScale = Vector3.Lerp(text.transform.localScale, originalScale, t / punchTime);
            yield return null;
        }

        text.transform.localScale = originalScale;
        text.color = originalColor;
    }

    public void ResetUI()
    {
        scoreValueText.text = "0";
        comboValueText.text = "";
        lastHitText.alpha = 0;

        lastScore = 0;
        lastGoodCombo = 0;
        lastPerfectCombo = 0;
        hasHitOccurred = false;
    }
}
