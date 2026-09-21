#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreviewChecks : MonoBehaviour
{
    private bool failed;
    private bool finishing;
    private float deadline;
    private bool hadScore;
    private int savedScore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Begin()
    {
        if (!Environment.GetCommandLineArgs().Contains("--preview-checks")) return;
        var go = new GameObject("PreviewChecks"); DontDestroyOnLoad(go); go.AddComponent<PreviewChecks>();
    }

    private void Awake()
    {
        hadScore = PlayerPrefs.HasKey("FourOperations.Score"); savedScore = PlayerPrefs.GetInt("FourOperations.Score");
        deadline = Time.realtimeSinceStartup + 120;
        Application.logMessageReceived += OnLog;
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
    }

    private void Update()
    {
        if (!finishing && (failed || Time.realtimeSinceStartup > deadline)) Finish(false);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("PREVIEW CHECK FAILED: " + message);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1);
        Check(SceneManager.GetActiveScene().name == "ModeSelect", "menu is entry scene");
        var links = FindObjectsByType<ModeNavigation>(FindObjectsSortMode.None);
        Check(links.Length == 7, "seven menu modes");
        Check(links.All(l => l.GetComponent<Image>().raycastTarget), "mode cards accept pointer input");
        Capture("mode-selection.png");
        string[] modes = { "Toplama", "Cikarma", "Carpma", "Bolme", "ToplamaCikarma", "CarpmaBolme", "DortIslem" };
        int checkedQuestions = 0;
        foreach (string mode in modes)
        {
            var link = FindObjectsByType<ModeNavigation>(FindObjectsSortMode.None).Single(l => l.targetScene == mode);
            link.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            Check(SceneManager.GetActiveScene().name == mode, "menu navigation: " + mode);
            var manager = FindFirstObjectByType<StemGameManager>();
            var math = manager.GetComponent<MathGame>();
            Check(math && math.enabled, "game initializes: " + mode);
            var buttons = manager.trueObjects[0].GetComponentsInChildren<Button>();
            int correct = ExpectedAnswer(manager);
            CheckChoices(buttons, correct); checkedQuestions++;
            if (mode == "Toplama" || mode == "Bolme" || mode == "DortIslem") Capture(mode + ".png");
            var wrong = buttons.First(b => int.Parse(b.GetComponentInChildren<TMP_Text>().text) != correct);
            wrong.onClick.Invoke(); wrong.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            Check(manager.gamedata.Data[0].Wrong == 1, "wrong-answer feedback/debounce: " + mode);
            for (int question = 0; question < manager.step; question++)
            {
                correct = ExpectedAnswer(manager); CheckChoices(buttons, correct); checkedQuestions++;
                var answer = buttons.Single(b => int.Parse(b.GetComponentInChildren<TMP_Text>().text) == correct);
                var remaining = typeof(MathGame).GetField("remaining", BindingFlags.NonPublic | BindingFlags.Instance);
                int before = (int)remaining.GetValue(math);
                answer.onClick.Invoke(); answer.onClick.Invoke();
                Check((int)remaining.GetValue(math) == before - 1, "correct-answer debounce: " + mode);
                yield return new WaitForSecondsRealtime(question == manager.step - 1 ? 1.7f : 0.4f);
            }
            Check(manager.levelIndex == 1, "completion advances one level: " + mode);
            CheckChoices(buttons, ExpectedAnswer(manager)); checkedQuestions++;
            GameObject.Find("Restart").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Check(FindFirstObjectByType<StemGameManager>().levelIndex == 0, "restart resets session: " + mode);
            GameObject.Find("Modes").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Check(SceneManager.GetActiveScene().name == "ModeSelect", "return to mode menu: " + mode);
        }
        Debug.Log("Checked " + checkedQuestions + " visible questions across all seven modes.");
        Finish(!failed);
    }

    private static int ExpectedAnswer(StemGameManager manager)
    {
        string a = manager.trueObjects[1].GetComponentInChildren<TMP_Text>().text;
        string b = manager.trueObjects[2].GetComponentInChildren<TMP_Text>().text;
        string c = manager.trueObjects[3].GetComponentInChildren<TMP_Text>().text;
        string op = manager.trueObjects[4].GetComponentInChildren<TMP_Text>().text;
        Check(new[] { a, b, c }.Count(v => v == "?") == 1, "one missing term");
        Check(manager.answerBoxesString.Contains(op), "operator belongs to selected mode");
        if (a == "?")
        {
            int y = int.Parse(b), z = int.Parse(c);
            return op == "+" ? z - y : op == "-" ? z + y : op == "x" ? z / y : z * y;
        }
        if (b == "?")
        {
            int x = int.Parse(a), z = int.Parse(c);
            return op == "+" ? z - x : op == "-" ? x - z : op == "x" ? z / x : x / z;
        }
        int first = int.Parse(a), second = int.Parse(b);
        return op == "+" ? first + second : op == "-" ? first - second : op == "x" ? first * second : first / second;
    }

    private static void CheckChoices(Button[] buttons, int expected)
    {
        var choices = buttons.Select(b => int.Parse(b.GetComponentInChildren<TMP_Text>().text)).ToArray();
        Check(choices.Length == 4 && choices.Distinct().Count() == 4, "four unique choices");
        Check(choices.Count(v => v == expected) == 1, "exactly one correct option");
        Check(buttons.All(b => b.interactable), "answer buttons unlocked");
    }

    private static void Capture(string filename)
    {
        var camera = Camera.main;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
        foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
        Canvas.ForceUpdateCanvases();
        var rt = new RenderTexture(1440, 900, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(1440, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); image.Apply();
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Docs")); Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, filename), image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null; Destroy(rt); Destroy(image);
        foreach (var canvas in canvases) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    private void Finish(bool success)
    {
        finishing = true;
        if (hadScore) PlayerPrefs.SetInt("FourOperations.Score", savedScore); else PlayerPrefs.DeleteKey("FourOperations.Score");
        PlayerPrefs.Save();
        Debug.Log(success ? "STEM_PREVIEW_CHECKS_OK" : "STEM_PREVIEW_CHECKS_FAILED");
        Application.Quit(success ? 0 : 1);
    }

    private void OnDestroy() { Application.logMessageReceived -= OnLog; }
}
#endif
