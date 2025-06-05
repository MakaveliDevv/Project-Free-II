// using TMPro;
// using UnityEngine;
// using System.Collections;

// public class ScoreDisplay : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private TMP_Text scoreText;
//     [SerializeField] private TMP_Text comboText;
//     [SerializeField] private TMP_Text lastHitText;

//     [Header("Animation Settings")]
//     [SerializeField] private Color defaultComboColor = Color.white;
//     [SerializeField] private Color boostComboColor = Color.yellow;
//     [SerializeField] private float punchScale = 1.3f;
//     [SerializeField] private float punchTime = 0.25f;
//     [SerializeField] private float fadeOutDelay = 3f;

//     private RewardSystem rewardSystem;

//     private int lastGoodCombo = 0;
//     private int lastPerfectCombo = 0;

//     private Coroutine fadeCoroutine;

//     void Start()
//     {
//         rewardSystem = FindFirstObjectByType <RewardSystem>();
//         if (rewardSystem == null)
//             Debug.LogError("⚠️ RewardSystem not found!");

//         UpdateUI(Attackable.HitResult.None);
//     }

//     public void UpdateUI(Attackable.HitResult hitResult)
//     {
//         if (fadeCoroutine != null)
//             StopCoroutine(fadeCoroutine);

//         // Update Texts
//         scoreText.text = $"Score: {rewardSystem.TotalScore}";

//         comboText.text =
//             $"Good Combo x{rewardSystem.GoodComboCount} (Best: {rewardSystem.HighestGoodCombo})\n" +
//             $"Perfect Combo x{rewardSystem.PerfectComboCount} (Best: {rewardSystem.HighestPerfectCombo})";

//         lastHitText.text = $"Last Hit: {hitResult}";

//         // Detect Combo Boost
//         if (rewardSystem.GoodComboCount > lastGoodCombo || rewardSystem.PerfectComboCount > lastPerfectCombo)
//         {
//             StartCoroutine(AnimateComboPunch());
//         }

//         lastGoodCombo = rewardSystem.GoodComboCount;
//         lastPerfectCombo = rewardSystem.PerfectComboCount;

//         fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
//     }

//     private IEnumerator AnimateComboPunch()
//     {
//         comboText.color = boostComboColor;

//         Vector3 originalScale = comboText.transform.localScale;
//         comboText.transform.localScale = originalScale * punchScale;

//         float t = 0;
//         while (t < punchTime)
//         {
//             t += Time.deltaTime;
//             comboText.transform.localScale = Vector3.Lerp(comboText.transform.localScale, originalScale, t / punchTime);
//             yield return null;
//         }

//         comboText.transform.localScale = originalScale;
//         comboText.color = defaultComboColor;
//     }

//     private IEnumerator FadeOutAfterDelay()
//     {
//         yield return new WaitForSeconds(fadeOutDelay);

//         scoreText.alpha = 0;
//         comboText.alpha = 0;
//         lastHitText.alpha = 0;
//     }

//     public void ResetUI()
//     {
//         scoreText.alpha = 1;
//         comboText.alpha = 1;
//         lastHitText.alpha = 1;

//         lastGoodCombo = 0;
//         lastPerfectCombo = 0;
//     }
// }


using TMPro;
using UnityEngine;
using System.Collections;

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
    [SerializeField] private float fadeOutDelay = 2f;

    private RewardSystem rewardSystem;

    private int lastGoodCombo = 0;
    private int lastPerfectCombo = 0;

    private Coroutine fadeCoroutine;

    void Start()
    {
        rewardSystem = FindFirstObjectByType<RewardSystem>();
        if (rewardSystem == null)
            Debug.LogError("⚠️ RewardSystem not found!");

        UpdateUI(Attackable.HitResult.None);
    }

    public void UpdateUI(Attackable.HitResult hitResult)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Set Labels
        scoreLabelText.text = "Score";
        comboLabelText.text = "Combo Count";

        // Update Values
        scoreValueText.text = rewardSystem.TotalScore.ToString();

        int totalCombo = Mathf.Max(rewardSystem.GoodComboCount, rewardSystem.PerfectComboCount);
        comboValueText.text = totalCombo >= 2 ? $"{totalCombo}x" : "—";

        lastHitText.text = hitResult.ToString();

        // Animate if combo increased
        if (rewardSystem.GoodComboCount > lastGoodCombo || rewardSystem.PerfectComboCount > lastPerfectCombo)
        {
            StartCoroutine(AnimateComboPunch());
        }

        lastGoodCombo = rewardSystem.GoodComboCount;
        lastPerfectCombo = rewardSystem.PerfectComboCount;

        // Only show if a combo is active
        if (totalCombo >= 2)
        {
            SetUIVisible(true);
            fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
        }
    }

    private IEnumerator AnimateComboPunch()
    {
        comboValueText.color = boostComboColor;

        Vector3 originalScale = comboValueText.transform.localScale;
        comboValueText.transform.localScale = originalScale * punchScale;

        float t = 0;
        while (t < punchTime)
        {
            t += Time.deltaTime;
            comboValueText.transform.localScale = Vector3.Lerp(comboValueText.transform.localScale, originalScale, t / punchTime);
            yield return null;
        }

        comboValueText.transform.localScale = originalScale;
        comboValueText.color = defaultComboColor;
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(fadeOutDelay);
        SetUIVisible(false);
    }

    private void SetUIVisible(bool visible)
    {
        float alpha = visible ? 1f : 0f;

        scoreLabelText.alpha = alpha;
        scoreValueText.alpha = alpha;
        comboLabelText.alpha = alpha;
        comboValueText.alpha = alpha;
        lastHitText.alpha = alpha;
    }

    public void ResetUI()
    {
        SetUIVisible(true);
        lastGoodCombo = 0;
        lastPerfectCombo = 0;
    }
}
