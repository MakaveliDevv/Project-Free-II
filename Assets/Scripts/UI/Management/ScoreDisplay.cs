// using TMPro;
// using UnityEngine;

// public class ScoreDisplay : MonoBehaviour
// {
//     [SerializeField] private RewardSystem rewardSystem;
//     [Header("References")]
//     [SerializeField] private TMP_Text scoreText;
//     [SerializeField] private TMP_Text comboText;
//     [SerializeField] private TMP_Text lastHitText;


//     void Start()
//     {
//         if (rewardSystem == null)
//             Debug.LogError("⚠️ RewardSystem not found in scene!");

//         UpdateUI(Attackable.HitResult.None);
//     }

//     public void UpdateUI(Attackable.HitResult hitResult)
//     {
//         scoreText.text = $"Score: {rewardSystem.TotalScore}";
//         comboText.text = $"Combo x{rewardSystem.ComboMultiplier}";
//         lastHitText.text = $"Last Hit: {hitResult}";
//     }
// }


using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private RewardSystem rewardSystem;

    [Header("References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text lastHitText;

    [Header("Animation Settings")]
    [SerializeField] private Color defaultComboColor = Color.white;
    [SerializeField] private Color boostComboColor = Color.yellow;
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchTime = 0.25f;
    [SerializeField] private float fadeOutDelay = 3f;

    private int lastCombo = 1;

    private Coroutine fadeCoroutine;

    void Start()
    {
        if (rewardSystem == null)
            Debug.LogError("⚠️ RewardSystem not found!");

        UpdateUI(Attackable.HitResult.None);
    }

    public void UpdateUI(Attackable.HitResult hitResult)
    {
        // Cancel any fade reset
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Update texts
        scoreText.text = $"Score: {rewardSystem.TotalScore}";
        comboText.text = $"Combo x{rewardSystem.ComboMultiplier}";
        lastHitText.text = $"Last Hit: {hitResult}";

        // Animate combo if increased
        if (rewardSystem.ComboMultiplier > lastCombo)
        {
            StartCoroutine(AnimateCombo());
        }

        lastCombo = rewardSystem.ComboMultiplier;

        // Restart fade out timer
        fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
    }

    private IEnumerator AnimateCombo()
    {
        comboText.color = boostComboColor;

        Vector3 originalScale = comboText.transform.localScale;
        comboText.transform.localScale = originalScale * punchScale;

        float t = 0;
        while (t < punchTime)
        {
            t += Time.deltaTime;
            comboText.transform.localScale = Vector3.Lerp(comboText.transform.localScale, originalScale, t / punchTime);
            yield return null;
        }

        comboText.transform.localScale = originalScale;
        comboText.color = defaultComboColor;
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(fadeOutDelay);

        scoreText.alpha = 0;
        comboText.alpha = 0;
        lastHitText.alpha = 0;
    }

    public void ResetUI()
    {
        scoreText.alpha = 1;
        comboText.alpha = 1;
        lastHitText.alpha = 1;
        lastCombo = 1;
    }
}
