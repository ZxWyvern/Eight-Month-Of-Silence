using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutupMata : MonoBehaviour
{
    [Header("Referensi Panel (Eyelid)")]
    [SerializeField] private RectTransform eyelidTop;
    [SerializeField] private RectTransform eyelidBottom;

    [Header("Timing")]
    [SerializeField] private float closeDuration = 1.8f;
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float delayBeforeLoad = 0.3f;

    private Vector2 topStartPos, topEndPos;
    private Vector2 bottomStartPos, bottomEndPos;
    private bool isTransitioning = false;

    void Start()
    {
        topStartPos = eyelidTop.anchoredPosition;
        bottomStartPos = eyelidBottom.anchoredPosition;

        topEndPos = new Vector2(topStartPos.x, 0f);
        bottomEndPos = new Vector2(bottomStartPos.x, 0f);
    }

    public void PlayTransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(CloseEyelidsAndLoad(sceneName));
    }

    private IEnumerator CloseEyelidsAndLoad(string sceneName)
    {
        isTransitioning = true;
        float t = 0f;

        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float progress = closeCurve.Evaluate(t / closeDuration);

            eyelidTop.anchoredPosition = Vector2.Lerp(topStartPos, topEndPos, progress);
            eyelidBottom.anchoredPosition = Vector2.Lerp(bottomStartPos, bottomEndPos, progress);

            yield return null;
        }

        eyelidTop.anchoredPosition = topEndPos;
        eyelidBottom.anchoredPosition = bottomEndPos;

        yield return new WaitForSeconds(delayBeforeLoad);

        SceneManager.LoadScene(sceneName);
    }
}