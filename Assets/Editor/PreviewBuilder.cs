using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PreviewBuilder
{
    private static TMP_FontAsset font;
    private static readonly Color Ink = Hex("163C3A");
    private static readonly Color Muted = Hex("617771");
    private static readonly Color Background = Hex("F2F5EF");
    private static readonly Color[] Accents = { Hex("167B65"), Hex("306AAF"), Hex("B46D11"), Hex("8051AE"), Hex("167B65"), Hex("8051AE"), Hex("163C3A") };
    private static readonly string[] Names = { "Toplama", "Cikarma", "Carpma", "Bolme", "ToplamaCikarma", "CarpmaBolme", "DortIslem" };
    private static readonly string[] Titles = { "Toplama", "Çıkarma", "Çarpma", "Bölme", "Toplama + Çıkarma", "Çarpma + Bölme", "Dört İşlem" };
    private static readonly string[] Symbols = { "+", "−", "×", "÷", "+  −", "×  ÷", "+  −  ×  ÷" };
    private static readonly string[] Descriptions = { "Sayıları bir araya getir.", "Farkı keşfet.", "Katlarıyla düşün.", "Eşit parçalara ayır.", "İki işlem, yeni sorular.", "Çarpanlar ve paylar.", "Tüm işlemler bir arada." };

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Game/Generated");
        Directory.CreateDirectory("Assets/Game/Scenes");
        ConfigureSettings();
        CreateFont();
        var correct = Feedback("Correct", "Doğru!", Hex("167B65"), false, false);
        var wrong = Feedback("Wrong", "Tekrar dene", Hex("B64D43"), true, false);
        var final = Feedback("Final", "Harika! Yeni seviye", Hex("167B65"), false, true);
        Menu();
        var config = JsonUtility.FromJson<ModeConfiguration>(File.ReadAllText("Assets/Resources/ModeConfiguration.json"));
        for (int i = 0; i < Names.Length; i++)
            Game(i, config.modes.Single(m => m.scene == Names[i]), correct, wrong, final);
        EditorBuildSettings.scenes = new[] { "ModeSelect" }.Concat(Names)
            .Select(n => new EditorBuildSettingsScene("Assets/Game/Scenes/" + n + ".unity", true)).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Game/Scenes/ModeSelect.unity");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
            locationPathName = "Builds/Windows/FourOperations.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Preview build failed");
        Debug.Log("STEM_PREVIEW_BUILD_OK");
    }

    private static void ConfigureSettings()
    {
        PlayerSettings.companyName = "Ufuk Bayhan";
        PlayerSettings.productName = "Dort Islem Preview";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultScreenWidth = 1440;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.ufukbayhan.fouroperations.preview");
        EditorSettings.serializationMode = SerializationMode.ForceText;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        settings.FindProperty("activeInputHandler").intValue = 0;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateFont()
    {
        const string path = "Assets/Game/Generated/InterfaceFont.asset";
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font) return;
        font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf"));
        font.name = "Interface Font";
        AssetDatabase.CreateAsset(font, path);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
    }

    private static RectTransform SceneCanvas()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = Background;
        camera.GetComponent<Camera>().orthographic = true;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var root = Canvas("Interface", 0);
        var bg = Panel(root, "Background", 0, 0, 1440, 900, Background);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        return root;
    }

    private static RectTransform Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return go.GetComponent<RectTransform>();
    }

    private static void Menu()
    {
        var root = SceneCanvas();
        Text(root, "Eyebrow", "MATEMATİK ATÖLYESİ", 72, 52, 900, 30, 18, Muted, true);
        Text(root, "Title", "Dört İşlem", 68, 102, 1000, 90, 72, Ink, true);
        Text(root, "Intro", "Bir mod seç. Eksik sayıyı bul. Her soruda bir adım ilerle.", 72, 207, 1200, 44, 25, Muted);
        var badge = Panel(root, "ModeCount", 1176, 122, 192, 48, Ink);
        Text(badge, "Label", "7 OYUN MODU", 0, 0, 192, 48, 17, Color.white, true, TextAlignmentOptions.Center);
        for (int i = 0; i < Names.Length; i++)
        {
            bool basic = i < 4;
            float width = basic ? 306 : 416;
            float x = 72 + (basic ? i : i - 4) * (width + 24);
            float y = basic ? 306 : 546;
            float height = basic ? 212 : 172;
            Panel(root, "Shadow", x, y + 5, width, height, Hex("E1E8DF"));
            var card = Panel(root, "Mode_" + Names[i], x, y, width, height, Color.white);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            card.GetComponent<Image>().raycastTarget = true;
            var nav = card.gameObject.AddComponent<ModeNavigation>(); nav.targetScene = Names[i];
            UnityEventTools.AddPersistentListener(button.onClick, nav.Open);
            Panel(card, "Accent", 0, 0, 5, height, Accents[i]);
            Text(card, "Symbol", Symbols[i], 26, 14, width - 52, 65, basic ? 56 : 37, Accents[i], true);
            Text(card, "Title", Titles[i], 26, basic ? 95 : 79, width - 50, 45, basic ? 30 : 26, Ink, true);
            Text(card, "Description", Descriptions[i], 26, basic ? 152 : 126, width - 48, 30, 19, Muted);
        }
        Text(root, "Hint", "NASIL OYNANIR?", 72, 775, 450, 30, 17, Ink, true);
        Text(root, "Help", "İşlemdeki ? işaretini tamamlayan cevaba tıkla veya dokun.", 72, 811, 1296, 32, 22, Muted);
        Save("ModeSelect");
    }

    private static void Game(int index, ModeSettings mode, GameObject correct, GameObject wrong, GameObject final)
    {
        var root = SceneCanvas();
        var managerObject = new GameObject("StemGameManager");
        var stem = managerObject.AddComponent<StemGameManager>();
        stem.stemGameType = StemGameType.MathGame;
        stem.answerBoxesString = mode.operators;
        stem.numberRangeXY = new Vector2Int(mode.x, mode.y);
        stem.step = mode.goal;
        stem.WinSFX = correct; stem.WrongSFX = wrong; stem.WinSFXFinal = final;
        var back = Button(root, "Modes", "<  Modlar", 72, 44, 166, 48, Color.white, Ink, 22);
        UnityEventTools.AddPersistentListener(back.onClick, stem.LoadBackScene);
        Text(root, "ModeHeading", Titles[index], 272, 39, 700, 60, 38, Ink, true);
        var badge = Panel(root, "LevelBadge", 1190, 44, 178, 48, Accents[index]);
        var levelText = Text(badge, "Level", "SEVİYE 01", 0, 0, 178, 48, 19, Color.white, true, TextAlignmentOptions.Center);
        Panel(root, "Rule", 72, 117, 1296, 2, Hex("DCE5DB"));
        Text(root, "Prompt", "Eksik sayıyı bul.", 72, 151, 1296, 66, 46, Ink, true, TextAlignmentOptions.Center);
        stem.yonergeText = Text(root, "Instruction", "", 72, 226, 1296, 76, 23, Muted, false, TextAlignmentOptions.Center);
        var equation = Panel(root, "EquationPanel", 222, 331, 996, 203, Color.white);
        Panel(equation, "Accent", 0, 0, 6, 203, Accents[index]);
        var first = Number(equation, "FirstNumber", 78, Accents[index]);
        var operation = Text(equation, "Operation", "+", 260, 43, 110, 118, 60, Accents[index], true, TextAlignmentOptions.Center);
        var second = Number(equation, "SecondNumber", 400, Accents[index]);
        Text(equation, "Equals", "=", 585, 43, 110, 118, 60, Muted, false, TextAlignmentOptions.Center);
        var result = Number(equation, "Result", 728, Accents[index]);
        Text(root, "AnswerPrompt", "CEVABINI SEÇ", 72, 572, 1296, 32, 17, Muted, true, TextAlignmentOptions.Center);
        var answers = Rect(root, "Answers", 270, 626, 900, 104);
        for (int i = 0; i < 4; i++)
            Button(answers, "Answer" + i, "0", i * 232, 0, 204, 104, Accents[index], Color.white, 44);
        stem.trueObjects = new[] { answers.gameObject, first.gameObject, second.gameObject, result.gameObject, operation.gameObject };
        var hud = managerObject.AddComponent<SessionHud>(); hud.manager = stem; hud.level = levelText;
        hud.timer = Text(root, "Timer", "SÜRE  00:00", 72, 814, 260, 36, 19, Muted);
        hud.mistakes = Text(root, "Mistakes", "HATA  0", 365, 814, 260, 36, 19, Muted);
        var restart = Button(root, "Restart", "Yeniden başla", 1140, 804, 228, 48, Color.white, Ink, 20);
        UnityEventTools.AddPersistentListener(restart.onClick, hud.Restart);
        Save(mode.scene);
    }

    private static TMP_Text Number(Transform parent, string name, float x, Color accent)
    {
        return Text(parent, name, "?", x, 43, 176, 118, 72, accent, true, TextAlignmentOptions.Center);
    }

    private static GameObject Feedback(string name, string label, Color color, bool wrong, bool final)
    {
        var root = Canvas(name + "SFX", 100);
        var card = Panel(root, "Toast", 460, 750, 520, 52, color);
        Text(card, "Message", label, 8, 0, 504, 52, 23, Color.white, true, TextAlignmentOptions.Center);
        foreach (var graphic in root.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        var fx = root.gameObject.AddComponent<FeedbackEffect>(); fx.wrong = wrong; fx.final = final; fx.lifetime = final ? 1.2f : 0.7f;
        string audioPath = "Assets/Game/Generated/" + name + ".wav";
        WriteTone(audioPath, wrong ? 220 : final ? 660 : 520, final ? 0.9f : 0.22f);
        AssetDatabase.ImportAsset(audioPath);
        var audio = root.gameObject.AddComponent<AudioSource>();
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath); audio.volume = 0.12f; audio.playOnAwake = true;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Game/Generated/" + name + "SFX.prefab");
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        return prefab;
    }

    private static void WriteTone(string path, float frequency, float duration)
    {
        const int rate = 22050;
        int count = (int)(duration * rate);
        using var w = new BinaryWriter(File.Create(path));
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + count * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(count * 2);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / rate;
            float envelope = Mathf.Min(1, t * 40) * Mathf.Pow(1 - (float)i / count, 2);
            w.Write((short)(Mathf.Sin(t * frequency * Mathf.PI * 2) * envelope * 12000));
        }
    }

    private static void Save(string name)
    {
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Game/Scenes/" + name + ".unity");
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var rt = Rect(parent, name, x, y, w, h); var image = rt.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rt;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var rt = Rect(parent, name, x, y, w, h); var text = rt.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.color = color; text.text = value;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; text.alignment = alignment; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true; text.fontSizeMin = size * 0.7f; text.fontSizeMax = size;
        return text;
    }

    private static Button Button(Transform parent, string name, string label, float x, float y, float w, float h, Color fill, Color color, float size)
    {
        var rt = Panel(parent, name, x, y, w, h, fill); var image = rt.GetComponent<Image>(); image.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Text(rt, "Label", label, 8, 0, w - 16, h, size, color, true, TextAlignmentOptions.Center); return button;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color); return color;
    }
}
