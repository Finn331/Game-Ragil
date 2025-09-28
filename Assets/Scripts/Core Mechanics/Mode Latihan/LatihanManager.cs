using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LatihanManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        [TextArea(2, 4)] public string prompt;
        public Sprite promptImage;
        public string[] choices = new string[4];
        [Range(0, 3)] public int correctIndex;
    }

    // ---- Custom UnityEvents ----
    [System.Serializable] public class IntIntEvent : UnityEvent<int, int> { } // (score, total)
    [System.Serializable] public class FloatEvent : UnityEvent<float> { } // percent 0..100 (float)
    [System.Serializable] public class IntEvent : UnityEvent<int> { } // grade 0..100 (int)

    #region Data & Pengaturan
    [Header("Question List")]
    public Question[] questionBank;

    [Header("Session Settings")]
    [Min(1)] public int sessionQuestionCount = 15;
    public bool shuffleQuestions = true;
    public bool shuffleChoices = false;
    public float autoNextDelay = 2f;

    [Header("UI Refs")]
    public RectTransform questionRoot;   // parent/container teks+gambar soal
    public TMP_Text questionText;
    public Image questionImage;
    public GameObject questionImageRoot;
    public Button[] answerButtons;       // A-D
    public TMP_Text[] answerLabels;      // label A-D
    public TMP_Text progressText;
    public TMP_Text scoreText;           // menampilkan "Nilai: X"

    [Header("Warna Highlight")]
    public Color defaultColor = Color.white;
    public Color correctColor = new Color(0.18f, 0.78f, 0.34f);
    public Color wrongColor = new Color(0.90f, 0.25f, 0.25f);

    [Header("Anim Settings")]
    public float inDuration = 0.25f;
    public float outDuration = 0.18f;
    public float staggerEach = 0.05f;  // jeda kemunculan per tombol
    public float punchScale = 1.08f;  // punch saat benar
    public float shakeDeg = 10f;    // goyangan saat salah

    [Header("Events (dipicu saat sesi selesai)")]
    public UnityEvent onSessionCompleted;
    public IntIntEvent onSessionCompletedScore;    // (scoreBenar, totalSoal)
    public FloatEvent onSessionCompletedPercent;  // percent float 0..100
    public IntEvent onSessionCompletedGrade;    // grade int 0..100

    [Header("Events (opsional)")]
    public UnityEvent onSessionRestarted;
    #endregion

    readonly List<int> _sessionOrder = new List<int>();
    int _currentIdxInSession = -1;
    int _score = 0;                   // jumlah jawaban benar
    bool _awaitingAnswer = false;
    int _currentCorrectBtn = -1;

    struct ChoicePack { public string text; public bool isCorrect; }
    ChoicePack[] _currentChoices = new ChoicePack[4];

    void Awake()
    {
        // binding klik tombol & siapkan CanvasGroup untuk anim
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int captured = i;
            if (answerButtons[i] == null) continue;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
            EnsureCanvasGroup(answerButtons[i].transform as RectTransform);
        }

        EnsureCanvasGroup(questionRoot);
    }

    void Start()
    {
        StartSession();
    }

    #region Sesi / Navigasi
    public void StartSession()
    {
        _score = 0;
        BuildSessionOrder();
        _currentIdxInSession = -1;
        NextQuestion();
        UpdateScoreUI(); // tampilkan nilai awal
    }

    void BuildSessionOrder()
    {
        _sessionOrder.Clear();
        int available = questionBank != null ? questionBank.Length : 0;
        if (available == 0)
        {
            Debug.LogWarning("[LatihanManager] Question Bank kosong.");
            return;
        }

        int take = Mathf.Clamp(sessionQuestionCount, 1, available);
        List<int> all = new List<int>(available);
        for (int i = 0; i < available; i++) all.Add(i);

        if (shuffleQuestions)
        {
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
        }

        for (int i = 0; i < take; i++) _sessionOrder.Add(all[i]);
    }

    void NextQuestion()
    {
        if (_sessionOrder.Count == 0) return;

        _currentIdxInSession++;
        if (_currentIdxInSession >= _sessionOrder.Count)
        {
            // === Sesi selesai ===
            int total = _sessionOrder.Count;
            if (progressText) progressText.text = $"{total}/{total}";
            SetAllAnswerInteractable(false);

            // Hitung nilai akhir
            float percentF = total > 0 ? (100f * _score / total) : 0f;
            int grade = Mathf.RoundToInt(percentF);
            if (scoreText) scoreText.text = $"Nilai: {grade}";

            // Trigger custom events
            onSessionCompleted?.Invoke();
            onSessionCompletedScore?.Invoke(_score, total);
            onSessionCompletedPercent?.Invoke(percentF);
            onSessionCompletedGrade?.Invoke(grade);

            Debug.Log($"[LatihanManager] Selesai. Benar: {_score}/{total}. Nilai: {grade}.");
            return;
        }

        var q = questionBank[_sessionOrder[_currentIdxInSession]];
        SetUI(q);

        ResetButtonColors();
        SetAllAnswerInteractable(true);
        _awaitingAnswer = true;

        if (progressText) progressText.text = $"{_currentIdxInSession + 1}/{_sessionOrder.Count}";

        PlayInAnimations();
        UpdateScoreUI(); // update nilai berjalan
    }
    #endregion

    #region UI & Interaksi
    void SetUI(Question q)
    {
        if (questionText) questionText.text = q.prompt;

        if (questionImage != null)
        {
            bool hasImg = q.promptImage != null;
            if (questionImageRoot) questionImageRoot.SetActive(hasImg);
            questionImage.enabled = hasImg;
            questionImage.sprite = q.promptImage;
            if (hasImg) questionImage.SetNativeSize();
        }

        // siapkan pilihan
        List<ChoicePack> packs = new List<ChoicePack>(4);
        for (int i = 0; i < 4; i++)
        {
            string txt = (i < q.choices.Length) ? q.choices[i] : null;
            if (!string.IsNullOrWhiteSpace(txt))
                packs.Add(new ChoicePack { text = txt, isCorrect = (i == q.correctIndex) });
        }

        if (shuffleChoices)
        {
            for (int i = packs.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (packs[i], packs[j]) = (packs[j], packs[i]);
            }
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            bool active = i < packs.Count;
            if (answerButtons[i] != null)
                answerButtons[i].gameObject.SetActive(active);
            if (answerLabels != null && i < answerLabels.Length && answerLabels[i] != null)
                answerLabels[i].transform.parent.gameObject.SetActive(active);

            if (active)
            {
                _currentChoices[i] = packs[i];
                if (answerLabels != null && i < answerLabels.Length && answerLabels[i] != null)
                    answerLabels[i].text = packs[i].text;

                if (answerButtons[i] != null && answerButtons[i].targetGraphic != null)
                    answerButtons[i].targetGraphic.canvasRenderer.SetColor(defaultColor);
            }
        }

        // cari index tombol benar
        _currentCorrectBtn = -1;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null &&
                answerButtons[i].gameObject.activeSelf &&
                _currentChoices[i].isCorrect)
            {
                _currentCorrectBtn = i;
                break;
            }
        }
    }

    void OnAnswerClicked(int buttonIndex)
    {
        if (!_awaitingAnswer) return;
        if (buttonIndex < 0 || buttonIndex >= answerButtons.Length) return;
        if (answerButtons[buttonIndex] == null ||
            !answerButtons[buttonIndex].gameObject.activeSelf) return;

        bool isCorrect = _currentChoices[buttonIndex].isCorrect;

        // highlight warna
        PaintButton(buttonIndex, isCorrect ? correctColor : wrongColor);
        if (!isCorrect && _currentCorrectBtn >= 0)
            PaintButton(_currentCorrectBtn, correctColor);

        // feedback anim tombol
        var rt = answerButtons[buttonIndex].transform as RectTransform;
        if (isCorrect) PlayPunch(rt);
        else PlayShake(rt);

        if (isCorrect) _score++;

        _awaitingAnswer = false;
        SetAllAnswerInteractable(false);

        // delay + anim out lalu lanjut soal
        StartCoroutine(DelayNextQuestion(autoNextDelay));

        UpdateScoreUI(); // update nilai berjalan setelah menjawab
    }
    #endregion

    #region Animations (LeanTween)
    void PlayInAnimations()
    {
        if (questionRoot != null)
        {
            LeanTween.cancel(questionRoot.gameObject);
            var cgQ = EnsureCanvasGroup(questionRoot);
            questionRoot.localScale = Vector3.one * 0.94f;
            questionRoot.anchoredPosition = new Vector2(questionRoot.anchoredPosition.x, -24f);
            cgQ.alpha = 0f;

            LeanTween.scale(questionRoot, Vector3.one, inDuration).setEaseOutBack();
            LeanTween.moveY(questionRoot, 0f, inDuration).setEaseOutCubic();
            LeanTween.alphaCanvas(cgQ, 1f, inDuration * 0.9f);
        }

        float baseDelay = 0.06f;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (btn == null || !btn.gameObject.activeSelf) continue;
            var rt = btn.transform as RectTransform;
            var cg = EnsureCanvasGroup(rt);

            LeanTween.cancel(rt.gameObject);
            rt.localScale = Vector3.one * 0.88f;
            cg.alpha = 0f;

            float d = baseDelay + i * staggerEach;
            LeanTween.scale(rt, Vector3.one, inDuration).setEaseOutBack().setDelay(d);
            LeanTween.alphaCanvas(cg, 1f, inDuration * 0.9f).setDelay(d);
        }
    }

    void PlayOutAnimations()
    {
        if (questionRoot != null)
        {
            var cgQ = EnsureCanvasGroup(questionRoot);
            LeanTween.scale(questionRoot, Vector3.one * 0.95f, outDuration).setEaseInCubic();
            LeanTween.alphaCanvas(cgQ, 0f, outDuration * 0.95f);
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            var btn = answerButtons[i];
            if (btn == null || !btn.gameObject.activeSelf) continue;
            var rt = btn.transform as RectTransform;
            var cg = EnsureCanvasGroup(rt);

            LeanTween.scale(rt, Vector3.one * 0.9f, outDuration).setEaseInCubic();
            LeanTween.alphaCanvas(cg, 0f, outDuration * 0.95f);
        }
    }

    void PlayPunch(RectTransform rt)
    {
        if (rt == null) return;
        LeanTween.cancel(rt.gameObject);
        Vector3 orig = Vector3.one;
        rt.localScale = orig;
        LeanTween.scale(rt, orig * punchScale, 0.12f).setEaseOutQuad()
                 .setOnComplete(() => LeanTween.scale(rt, orig, 0.12f).setEaseInQuad());
    }

    void PlayShake(RectTransform rt)
    {
        if (rt == null) return;
        LeanTween.cancel(rt.gameObject);
        float half = 0.06f;
        LeanTween.rotateZ(rt.gameObject, shakeDeg, half).setEaseOutQuad()
                 .setOnComplete(() =>
                    LeanTween.rotateZ(rt.gameObject, -shakeDeg, half).setEaseInOutQuad()
                             .setOnComplete(() =>
                                LeanTween.rotateZ(rt.gameObject, 0f, half).setEaseOutQuad()
                             ));
    }
    #endregion

    #region Helpers & Utility
    void UpdateScoreUI()
    {
        if (scoreText == null) return;
        int total = Mathf.Max(1, _sessionOrder.Count); // hindari div by zero
        int grade = Mathf.RoundToInt(100f * _score / total);
        scoreText.text = $"Nilai: {grade}";
    }

    IEnumerator DelayNextQuestion(float delay)
    {
        float stay = Mathf.Max(0f, delay - outDuration);
        yield return new WaitForSeconds(stay);
        PlayOutAnimations();
        yield return new WaitForSeconds(outDuration);
        NextQuestion();
    }

    CanvasGroup EnsureCanvasGroup(RectTransform rt)
    {
        if (rt == null) return null;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    void SetAllAnswerInteractable(bool value)
    {
        foreach (var b in answerButtons) if (b != null) b.interactable = value;
    }

    void ResetButtonColors()
    {
        foreach (var b in answerButtons)
            if (b != null && b.targetGraphic is Graphic g) g.color = defaultColor;
    }

    void PaintButton(int idx, Color c)
    {
        if (idx < 0 || idx >= answerButtons.Length) return;
        if (answerButtons[idx] != null && answerButtons[idx].targetGraphic is Graphic g)
            g.color = c;
    }

    /// <summary>
    /// Restart sesi latihan.
    /// Jika reshuffle=true, urutan soal diacak ulang; jika false, gunakan urutan sesi terakhir.
    /// </summary>
    public void Restart(bool reshuffle = true)
    {
        // Stop semua proses jalan
        StopAllCoroutines();

        // Cancel tween yang masih aktif
        if (questionRoot != null) LeanTween.cancel(questionRoot.gameObject);
        foreach (var b in answerButtons)
            if (b != null) LeanTween.cancel(b.gameObject);

        // Reset state
        _score = 0;
        _awaitingAnswer = false;
        _currentIdxInSession = -1;

        // Reset tampilan tombol (warna & interactable)
        ResetButtonColors();
        SetAllAnswerInteractable(false);

        // Opsi: reshuffle order atau pakai yang lama
        if (reshuffle || _sessionOrder.Count == 0)
        {
            BuildSessionOrder();
        }

        // Mulai lagi
        NextQuestion();
        UpdateScoreUI();

        // Trigger event opsional
        onSessionRestarted?.Invoke();
    }

    /// <summary>Restart dengan urutan yang sama (tanpa reshuffle).</summary>
    public void RestartSameOrder() => Restart(false);

    /// <summary>Restart dengan urutan diacak ulang (default).</summary>
    public void RestartReshuffle() => Restart(true);
    #endregion
}
