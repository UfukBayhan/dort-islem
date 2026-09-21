using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MathGame : MonoBehaviour
{
    private const float AnswerUnlockDelaySeconds = 0.24f;
    private const int MaxQuestionGenerationAttempts = 50;

    private StemGameManager stem;
    private TextMeshProUGUI yonergeText;

    private GameObject firstNumber;
    private GameObject secondNumber;
    private GameObject finalNumber;
    private GameObject islemText;
    private GameObject cevaplar;

    private readonly List<Button> cevapButonlari = new List<Button>();
    private readonly List<TextMeshProUGUI> cevapMetinleri = new List<TextMeshProUGUI>();

    private string[] operators;
    private int limitFirstNumbers = 10;
    private int limitFinalNumbers = 10;
    private int levelIndex;
    private int currentLevelForQuestions = -1;
    private int sonuc;
    private int sira;
    private int firstvar,
        secondvar,
        finalvar;

    private TextMeshProUGUI first,
        second,
        final;
    private TextMeshProUGUI operationText;
    private GameObject WinSFX;
    private GameObject WinSFXFinal;
    private GameObject WrongSFX;
    private GameObject answerFeedbackSfx;
    private GameObject SoundFX;
    private readonly HashSet<string> generatedQuestions = new HashSet<string>();

    // StemGameManager uyumluluk
    private bool levelTransitioning = false;
    private bool isAnswerLocked = false;
    private Coroutine answerUnlockRoutine;
    private readonly Dictionary<Button, ColorBlock> answerButtonColors =
        new Dictionary<Button, ColorBlock>();
    private readonly Dictionary<Button, UnityAction> answerButtonCallbacks =
        new Dictionary<Button, UnityAction>();
    private int remaining;
    private int goalCount = 5; // Stem’den set edilecek

    private void Start()
    {
        if (!TryInitializeComponents())
            return;

        CreateOperations();
    }

    private bool TryInitializeComponents()
    {
        // MathGame, StemGameManager tarafından aynı GameObject'e eklenir.
        // Sahne genelinde arama yapmak yerine doğrudan sahibi üzerinden al.
        stem = GetComponent<StemGameManager>();
        if (stem == null)
            return ConfigurationError("StemGameManager aynı GameObject üzerinde bulunamadı.");

        if (stem.trueObjects == null || stem.trueObjects.Length < 5)
            return ConfigurationError("trueObjects dizisinde 5 zorunlu UI referansı bulunmalı.");

        for (int i = 0; i < 5; i++)
        {
            if (stem.trueObjects[i] == null)
                return ConfigurationError($"trueObjects[{i}] referansı eksik.");
        }

        cevaplar = stem.trueObjects[0];
        firstNumber = stem.trueObjects[1];
        secondNumber = stem.trueObjects[2];
        finalNumber = stem.trueObjects[3];
        islemText = stem.trueObjects[4];
        levelIndex = stem.levelIndex;
        operators = stem.answerBoxesString;
        limitFirstNumbers = stem.numberRangeXY.x;
        limitFinalNumbers = stem.numberRangeXY.y;
        WinSFX = stem.WinSFX;
        WrongSFX = stem.WrongSFX;
        WinSFXFinal = stem.WinSFXFinal;
        yonergeText = stem.yonergeText;
        goalCount = stem.step;

        if (goalCount <= 0)
            return ConfigurationError("step değeri sıfırdan büyük olmalı.");
        if (operators == null || operators.Length == 0)
            return ConfigurationError("En az bir işlem operatörü tanımlanmalı.");
        if (operators.Any(op => op != "+" && op != "-" && op != "x" && op != "/"))
            return ConfigurationError("Operatörler yalnızca +, -, x veya / olabilir.");
        if (yonergeText == null)
            return ConfigurationError("yonergeText referansı eksik.");
        if (WrongSFX == null || WinSFX == null || WinSFXFinal == null)
            return ConfigurationError("WrongSFX, WinSFX ve WinSFXFinal referansları zorunlu.");

        if (!TryCacheText(firstNumber, "ilk sayı", out first))
            return false;
        if (!TryCacheText(secondNumber, "ikinci sayı", out second))
            return false;
        if (!TryCacheText(finalNumber, "sonuç", out final))
            return false;
        if (!TryCacheText(islemText, "işlem", out operationText))
            return false;

        cevapButonlari.Clear();
        cevapMetinleri.Clear();
        for (int i = 0; i < cevaplar.transform.childCount; i++)
        {
            Transform answerTransform = cevaplar.transform.GetChild(i);
            if (!answerTransform.TryGetComponent<Button>(out var button))
                return ConfigurationError($"Cevaplar altındaki {answerTransform.name} Button içermiyor.");

            TextMeshProUGUI answerText = answerTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (answerText == null)
                return ConfigurationError($"{answerTransform.name} için cevap metni bulunamadı.");

            cevapButonlari.Add(button);
            cevapMetinleri.Add(answerText);
        }

        if (cevapButonlari.Count == 0)
            return ConfigurationError("Cevaplar altında en az bir cevap butonu bulunmalı.");

        remaining = goalCount;
        UpdateYonergeText();
        return true;
    }

    private bool TryCacheText(GameObject owner, string referenceName, out TextMeshProUGUI text)
    {
        text = owner.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            return true;

        return ConfigurationError($"{referenceName} UI nesnesinde TMP metni bulunamadı.");
    }

    private bool ConfigurationError(string message)
    {
        Debug.LogError($"[MathGame] Yapılandırma hatası: {message}", this);
        enabled = false;
        return false;
    }

    private void CreateOperations()
    {
        if (levelIndex != currentLevelForQuestions)
        {
            generatedQuestions.Clear();
            currentLevelForQuestions = levelIndex;
        }

        string islemTuru = null;
        string questionSignature = null;
        bool questionGenerated = false;

        for (int attempt = 0; attempt < MaxQuestionGenerationAttempts; attempt++)
        {
            islemTuru = operators[Random.Range(0, operators.Length)];
            sira = Random.Range(1, 4); // 1..3

            if (!TryGenerateValues(islemTuru))
                continue;

            questionSignature = $"{firstvar}|{islemTuru}|{secondvar}|{finalvar}|{sira}";
            if (generatedQuestions.Add(questionSignature))
            {
                questionGenerated = true;
                break;
            }

            // Küçük soru havuzlarında oyunu kilitlemek yerine kontrollü tekrara izin ver.
            if (attempt == MaxQuestionGenerationAttempts - 1)
                questionGenerated = true;
        }

        if (!questionGenerated)
        {
            ConfigurationError("Geçerli bir matematik sorusu üretilemedi.");
            return;
        }

        operationText.text = islemTuru;
        SetupOperation(finalvar);
        sonuc = (sira == 1) ? firstvar : (sira == 2 ? secondvar : finalvar);
        smartAnswer(islemTuru);
    }

    private bool TryGenerateValues(string islemTuru)
    {
        switch (islemTuru)
        {
            case "+":
                int resultLimit = GetAdditiveResultLimit();
                int operandLimit = Mathf.Min(GetConfiguredOperandLimit(), resultLimit - 1);
                if (operandLimit < 1)
                    return false;

                firstvar = Random.Range(1, operandLimit + 1);
                int secondLimit = Mathf.Min(operandLimit, resultLimit - firstvar);
                if (secondLimit < 1)
                    return false;

                secondvar = Random.Range(1, secondLimit + 1);
                finalvar = firstvar + secondvar;
                return true;

            case "-":
                int subtractionLimit = GetConfiguredOperandLimit();
                firstvar = Random.Range(1, subtractionLimit + 1);
                secondvar = Random.Range(1, subtractionLimit + 1);
                if (firstvar < secondvar)
                    (firstvar, secondvar) = (secondvar, firstvar);
                finalvar = firstvar - secondvar;
                return true;

            case "x":
                int mulLimit = GetMulDivLimit(levelIndex);
                firstvar = Random.Range(1, mulLimit + 1);
                secondvar = Random.Range(1, mulLimit + 1);
                finalvar = firstvar * secondvar;
                return true;

            case "/":
                int divLimit = GetMulDivLimit(levelIndex);
                finalvar = Random.Range(1, divLimit + 1); // bölüm
                secondvar = Random.Range(1, divLimit + 1); // bölen
                firstvar = finalvar * secondvar; // bölünen; sonuç her zaman tam sayı
                return true;

            default:
                return false;
        }
    }

    private int GetConfiguredOperandLimit()
    {
        return Mathf.Max(1, limitFirstNumbers);
    }

    private int GetAdditiveResultLimit()
    {
        int fallback = GetConfiguredOperandLimit() * 2;
        return Mathf.Max(2, limitFinalNumbers > 0 ? limitFinalNumbers : fallback);
    }

    void SetupOperation(int result)
    {
        if (sira == 1)
        {
            first.text = "?";
            second.text = secondvar.ToString();
            final.text = result.ToString();
        }
        else if (sira == 2)
        {
            first.text = firstvar.ToString();
            second.text = "?";
            final.text = result.ToString();
        }
        else
        {
            first.text = firstvar.ToString();
            second.text = secondvar.ToString();
            final.text = "?";
        }
        finalvar = result;
    }

    private void smartAnswer(string islemTuru)
    {
        int n = cevapButonlari.Count;
        var pool = new HashSet<int> { sonuc };

        IEnumerable<int> guesses = Enumerable.Empty<int>();
        switch (islemTuru)
        {
            case "+":
                guesses = CloseTo(
                    (sira == 1) ? finalvar - secondvar
                    : (sira == 2) ? finalvar - firstvar
                    : firstvar + secondvar
                );
                break;
            case "-":
                guesses = CloseTo(sonuc);
                break;
            case "x":
                guesses =
                    (sira == 1) ? CloseTo(firstvar)
                    : (sira == 2) ? CloseTo(secondvar)
                    : MultCloseTo(finalvar, firstvar, secondvar);
                break;
            case "/":
                guesses =
                    (sira == 1) ? DivDividendCloseTo(finalvar, secondvar)
                    : (sira == 2) ? CloseTo(secondvar)
                    : CloseTo(finalvar);
                break;
        }

        foreach (var g in guesses)
        {
            if (pool.Count >= n)
                break;
            if (g > 0 && g != sonuc)
                pool.Add(g);
        }

        int guard = 0;
        int baseFill = Mathf.Max(1, sonuc);
        int step = 1;
        while (pool.Count < n && guard++ < 200)
        {
            int c1 = baseFill + step;
            int c2 = baseFill - step;
            if (c1 != sonuc)
                pool.Add(c1);
            if (pool.Count >= n)
                break;
            if (c2 > 0 && c2 != sonuc)
                pool.Add(c2);
            step++;
        }

        var options = pool.ToList();
        Shuffle(options);

        for (int i = 0; i < cevapButonlari.Count; i++)
        {
            int val = options[i];
            cevapMetinleri[i].text = val.ToString();
            Button btn = cevapButonlari[i];
            if (answerButtonCallbacks.TryGetValue(btn, out UnityAction previousCallback))
                btn.onClick.RemoveListener(previousCallback);

            UnityAction callback = () => HandleAnswer(val);
            answerButtonCallbacks[btn] = callback;
            btn.onClick.AddListener(callback);
        }
    }

    IEnumerable<int> CloseTo(int baseVal) =>
        new List<int> { baseVal - 1, baseVal - 2, baseVal + 1, baseVal + 2 }.Where(x => x > 0);

    IEnumerable<int> MultCloseTo(int result, int a, int b) =>
        new List<int>
        {
            b * Mathf.Max(1, a - 1),
            b * Mathf.Max(1, a - 2),
            b * (a + 1),
            b * (a + 2),
        }.Where(x => x > 0);

    IEnumerable<int> DivDividendCloseTo(int quotient, int divisor) =>
        new List<int>
        {
            (quotient - 1) * divisor,
            (quotient + 1) * divisor,
            (quotient - 2) * divisor,
            (quotient + 2) * divisor,
        }.Where(x => x > 0);

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void HandleAnswer(int answerValue)
    {
        // Aynı karede veya aynı anda gelen birden fazla dokunuştan yalnızca
        // ilkini kabul et. Yeni soru hazır olmadan sayaç tekrar değişmemeli.
        if (isAnswerLocked || levelTransitioning)
            return;

        isAnswerLocked = true;
        SetAnswerButtonsInteractable(false);
        ClearAnswerFeedbackSfx();

        if (answerValue != sonuc)
        {
            ShowAnswerFeedback(WrongSFX, "WrongSFX");
            ScheduleAnswerUnlock();
        }
        else
        {
            remaining = Mathf.Max(0, remaining - 1);
            if (remaining == 0)
            {
                levelTransitioning = true;
                stem.PauseLevelTimer();
                TriggerFinale();
                StartCoroutine(ResetLevelTransitionWhenSfxEnds());
            }
            else
            {
                ShowAnswerFeedback(WinSFX, "WinSFX");
                CreateOperations();
                ScheduleAnswerUnlock();
            }
        }
        UpdateYonergeText();
    }

    private void ShowAnswerFeedback(GameObject prefab, string instanceName)
    {
        ClearAnswerFeedbackSfx();
        answerFeedbackSfx = Instantiate(prefab);
        answerFeedbackSfx.name = instanceName;
    }

    private void ClearAnswerFeedbackSfx()
    {
        if (answerFeedbackSfx == null)
            return;

        Destroy(answerFeedbackSfx);
        answerFeedbackSfx = null;
    }

    private void ScheduleAnswerUnlock()
    {
        if (answerUnlockRoutine == null)
            answerUnlockRoutine = StartCoroutine(UnlockAnswersAfterDelay());
    }

    private IEnumerator UnlockAnswersAfterDelay()
    {
        // Aynı karede kuyruğa alınmış ikinci UI click olayını da engelle.
        yield return null;

        // touchCount bazı cihazlarda sıfırlanmayabildiği için kilidi giriş
        // durumuna bağlama. Kısa debounce çoklu tıklamayı engeller ve kilidin
        // her koşulda açılmasını sağlar. Time.timeScale'dan etkilenmez.
        yield return new WaitForSecondsRealtime(AnswerUnlockDelaySeconds);

        answerUnlockRoutine = null;
        isAnswerLocked = false;
        SetAnswerButtonsInteractable(true);
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (var button in cevapButonlari)
        {
            if (button == null)
                continue;

            if (!interactable)
            {
                if (!answerButtonColors.ContainsKey(button))
                    answerButtonColors[button] = button.colors;

                var colors = button.colors;
                colors.disabledColor = colors.normalColor;
                button.colors = colors;
                button.interactable = false;
            }
            else
            {
                if (answerButtonColors.TryGetValue(button, out var originalColors))
                    button.colors = originalColors;

                button.interactable = true;
            }
        }

        if (interactable)
            answerButtonColors.Clear();
    }

    private void TriggerFinale()
    {
        if (SoundFX != null)
        {
            Destroy(SoundFX);
            SoundFX = null;
        }

        SoundFX = Instantiate(WinSFXFinal);
        SoundFX.name = "WinSFXFinal";
    }

    private IEnumerator ResetLevelTransitionWhenSfxEnds()
    {
        // Finale aynı karede tamamlansa bile kuyruğa alınmış diğer click
        // olaylarının yeni seviyeye taşınmasına izin verme.
        yield return null;

        // SoundFX nesnesi ve AudioSource bileşeni var mı kontrol et
        if (SoundFX != null)
        {
            var audioSource = SoundFX.GetComponent<AudioSource>();

            // Eğer AudioSource varsa ve bir ses çalıyorsa, bitmesini bekle
            if (audioSource != null && audioSource.isPlaying)
            {
                // Prefab yanlışlıkla loop'a alınsa veya ses sistemi takılsa bile
                // seviye geçişi sonsuza kadar kilitlenmemeli.
                float clipDuration = audioSource.clip != null ? audioSource.clip.length : 0f;
                float timeoutAt = Time.realtimeSinceStartup + Mathf.Max(2f, clipDuration + 1f);
                while (audioSource != null && audioSource.isPlaying && Time.realtimeSinceStartup < timeoutAt)
                {
                    yield return null;
                }
            }
        }
        if (SoundFX != null)
        {
            Destroy(SoundFX);
            SoundFX = null;
        }
        // WinSFXFinal üzerindeki finalAnswerHelper normalde StemGameManager'ı
        // ilerletir. Prefab davranışı değişirse veri kaybı olmaması için güvenli geri dönüş.
        if (stem.levelIndex == levelIndex)
            stem.FinalAnswer();

        levelIndex = stem.levelIndex;
        stem.ResetCurrentLevelTimer();
        ApplyDifficultyWithGroup(levelIndex);

        remaining = goalCount;
        UpdateYonergeText();

        levelTransitioning = false;
        CreateOperations();
        ScheduleAnswerUnlock();
    }

    private void ApplyDifficultyWithGroup(int index)
    {
        int clampedIndex = Mathf.Min(index, 100);

        // Grup bilgisi (5’lik gruplar)
        int groupIndex = clampedIndex / 5;
        int groupStart = groupIndex * 5;
        int groupEnd = Mathf.Min(groupStart + 4, 100);
        float groupProgress = (clampedIndex % 5) / 4f;

        // Dalga (sinüs ile 0–1 arası dalgalanma)
        float wave = (Mathf.Sin(clampedIndex * Mathf.PI / 18f) + 1f) * 0.5f;

        // --- GOAL COUNT ---
        if (index <= 50)
        {
            int goalMin = GoalCurve(groupStart);
            int goalMax = GoalCurve(groupEnd);
            goalCount = Mathf.RoundToInt(LerpWithWave(goalMin, goalMax, wave, groupProgress));
        }
        else
        {
            goalCount = 20; // 50 sonrası sabit
        }

        // --- NUMBER RANGE ---
        if (index <= 50)
        {
            int rangeMin = RangeCurve(groupStart);
            int rangeMax = RangeCurve(groupEnd);
            int finalRange = Mathf.RoundToInt(
                LerpWithWave(rangeMin, rangeMax, wave, groupProgress)
            );
            limitFirstNumbers = finalRange;
            limitFinalNumbers = finalRange;
        }
        else
        {
            limitFirstNumbers = 999;
            limitFinalNumbers = 999;
        }

        Debug.Log($"[LEVEL {index}] Goal={goalCount}, Range={limitFirstNumbers}");
    }

    private int GetMulDivLimit(int level)
    {
        float up;

        if (level <= 9)
            up = Mathf.Lerp(5f, 10f, level / 10f); // Level 0–10 → 5 → 10
        else if (level <= 19)
            up = Mathf.Lerp(10f, 20f, (level - 10) / 10f); // Level 11–20 → 10 → 20
        else if (level <= 29)
            up = Mathf.Lerp(20f, 40f, (level - 20) / 10f); // Level 21–30 → 20 → 40
        else if (level <= 39)
            up = Mathf.Lerp(40f, 70f, (level - 30) / 10f); // Level 31–40 → 40 → 70
        else if (level <= 49)
            up = Mathf.Lerp(70f, 100f, (level - 40) / 10f); // Level 41–50 → 70 → 100
        else
            up = 100f; // 50 sonrası sabit

        // Küçük dalga (%5 oynama)
        float wave = (Mathf.Sin(level * Mathf.PI / 10f) + 1f) * 0.5f;
        float wobble = Mathf.Lerp(-0.05f, 0.05f, wave);
        up *= (1f + wobble);
        return Mathf.Clamp(Mathf.RoundToInt(up), 5, 100);
    }

    private int GoalCurve(int level)
    {
        float up;

        if (level <= 4)
            up = Mathf.Lerp(3f, 5f, level / 4f); // 0–4 → 3–5
        else if (level <= 9)
            up = Mathf.Lerp(5f, 8f, (level - 5) / 4f); // 5–9 → 5–8
        else if (level <= 19)
            up = Mathf.Lerp(8f, 10f, (level - 10) / 10f); // 10–19 → 8–10
        else if (level <= 29)
            up = Mathf.Lerp(10f, 12f, (level - 20) / 10f); // 20–29 → 10–12
        else if (level <= 39)
            up = Mathf.Lerp(12f, 15f, (level - 30) / 10f); // 30–39 → 12–15
        else if (level <= 49)
            up = Mathf.Lerp(15f, 20f, (level - 40) / 10f); // 40–49 → 15–20
        else
            up = 20f; // 50 sonrası sabit

        // Küçük dalga (%10 oynama)
        float wave = (Mathf.Sin(level * Mathf.PI / 10f) + 1f) * 0.5f;
        float wobble = Mathf.Lerp(-0.1f, 0.1f, wave);
        up *= (1f + wobble);

        return Mathf.Clamp(Mathf.RoundToInt(up), 3, 20);
    }

    private int RangeCurve(int level)
    {
        float up;

        if (level <= 10)
            up = Mathf.Lerp(20f, 100f, level / 10f); // Level 0–10 → 20 → 100
        else if (level <= 20)
            up = Mathf.Lerp(100f, 300f, (level - 10) / 10f); // Level 11–20 → 100 → 300
        else if (level <= 30)
            up = Mathf.Lerp(300f, 600f, (level - 20) / 10f); // Level 21–30 → 300 → 600
        else if (level <= 40)
            up = Mathf.Lerp(600f, 800f, (level - 30) / 10f); // Level 31–40 → 600 → 800
        else if (level <= 50)
            up = Mathf.Lerp(800f, 1000f, (level - 40) / 10f); // Level 41–50 → 800 → 1000
        else
            up = 1000f; // 50 sonrası sabit

        // Küçük dalga (%5 oynama)
        float wave = (Mathf.Sin(level * Mathf.PI / 12f) + 1f) * 0.5f;
        float wobble = Mathf.Lerp(-0.05f, 0.05f, wave);
        up *= (1f + wobble);

        return Mathf.Clamp(Mathf.RoundToInt(up), 10, 1000);
    }

    private float LerpWithWave(float min, float max, float wave, float t)
    {
        float baseLerp = Mathf.Lerp(min, max, t);
        return baseLerp + wave * (max - min) * 0.1f; // %10 oynatma
    }

    private void UpdateYonergeText()
    {
        yonergeText.text =
            $"Seviye {levelIndex + 1}\n"
            + $"{remaining} işlem daha doğru cevapla ve bir üst seviyeye geç!";
    }

    private void OnDestroy()
    {
        foreach (var pair in answerButtonCallbacks)
        {
            if (pair.Key != null)
                pair.Key.onClick.RemoveListener(pair.Value);
        }
        answerButtonCallbacks.Clear();

        ClearAnswerFeedbackSfx();

        if (SoundFX != null)
        {
            Destroy(SoundFX);
            SoundFX = null;
        }
    }
}
