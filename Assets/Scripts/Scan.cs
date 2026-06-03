using UnityEngine;
using System.Collections;
using Vuforia;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Scan : MonoBehaviour
{
    public Canvas uiCanvas;
    public Camera arCamera;
    public Button snapshotButton;
    public Button analyzeButton;
    public TextMeshProUGUI displayText;
    public RectTransform scanArea;
    public Analyze analyzePageManager;
    public ARVisualizationManager arVisualizationManager;

    private bool isProcessing = false;
    private TesseractDriver _tesseractDriver;
    private Texture2D _textureToProcess;
    private string _lastScannedText = "";

    void Start()
    {
        VuforiaApplication.Instance.Initialize();
        _tesseractDriver = new TesseractDriver();
        VuforiaApplication.Instance.OnVuforiaStarted += InitializeVuforiaCamera;

        snapshotButton.gameObject.SetActive(true);
        analyzeButton.gameObject.SetActive(false);
    }

    void InitializeVuforiaCamera()
    {
        VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(PixelFormat.RGB888, true);
    }

    public void TakeAShot()
    {
        if (isProcessing) return;
        
        StartCoroutine(CaptureAndProcess());
    }

    private IEnumerator CaptureAndProcess()
    {
        isProcessing = true;
        _lastScannedText = "";
        SetUILoading(true);
        yield return new WaitForEndOfFrame();

        // Coba ambil gambar dari kamera Vuforia
        Vuforia.Image cameraImage = null;
        try
        {
            cameraImage = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.RGB888);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[NutriLens] Tidak bisa ambil gambar kamera: " + e.Message);
        }

        // Kalau kamera null (laptop/editor), langsung skip ke analyze
        if (cameraImage == null)
        {
            Debug.LogWarning("[NutriLens] Camera image null - skip image processing, lanjut ke analyze.");
            isProcessing = false;
            SetUILoading(false);
            if (displayText != null) displayText.text = "OCR Result: (skipped)";
        }
        else
        {
            // ── Proses gambar kamera ──────────────────────────────────
            Rect screenRect = GetScreenRect(scanArea);
            int startX = Mathf.Clamp(Mathf.RoundToInt(screenRect.x), 0, Screen.width);
            int startY = Mathf.Clamp(Mathf.RoundToInt(screenRect.y), 0, Screen.height);
            int cropW  = Mathf.Clamp(Mathf.RoundToInt(screenRect.width),  1, Screen.width  - startX);
            int cropH  = Mathf.Clamp(Mathf.RoundToInt(screenRect.height), 1, Screen.height - startY);
            int outW   = Screen.width;
            int outH   = Screen.height;

            Texture2D rawBgTex = new Texture2D(cameraImage.Width, cameraImage.Height, TextureFormat.RGB24, false);
            cameraImage.CopyToTexture(rawBgTex);
            rawBgTex.Apply();

            RenderTexture bgRT = RenderTexture.GetTemporary(outW, outH, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(rawBgTex, bgRT);
            RenderTexture.active = bgRT;
            Texture2D bgTex = new Texture2D(cropW, cropH, TextureFormat.RGB24, false);
            bgTex.ReadPixels(new Rect(startX, startY, cropW, cropH), 0, 0);
            bgTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(bgRT);
            Destroy(rawBgTex);

            RenderTexture fgRT = new RenderTexture(outW, outH, 24, RenderTextureFormat.ARGB32);
            CameraClearFlags origFlags = arCamera.clearFlags;
            Color origColor = arCamera.backgroundColor;
            RenderTexture origTarget = arCamera.targetTexture;
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            arCamera.backgroundColor = new Color(0, 0, 0, 0);
            arCamera.targetTexture = fgRT;
            arCamera.Render();
            arCamera.targetTexture = origTarget;
            arCamera.clearFlags = origFlags;
            arCamera.backgroundColor = origColor;

            RenderTexture.active = fgRT;
            Texture2D fgTex = new Texture2D(cropW, cropH, TextureFormat.ARGB32, false);
            fgTex.ReadPixels(new Rect(startX, startY, cropW, cropH), 0, 0);
            fgTex.Apply();
            RenderTexture.active = null;

            Color[] bgPixels = bgTex.GetPixels();
            Color[] fgPixels = fgTex.GetPixels();
            for (int i = 0; i < bgPixels.Length; i++)
            {
                Color fg = fgPixels[i];
                bgPixels[i] = fg * fg.a + bgPixels[i] * (1f - fg.a);
            }

            Texture2D finalTex = new Texture2D(cropW, cropH);
            finalTex.SetPixels(bgPixels);
            finalTex.Apply();
            Destroy(bgTex);
            Destroy(fgTex);
            Destroy(fgRT);

            _textureToProcess = ImagePreprocessor.Preprocess(finalTex);
            SaveTextureForDebug(finalTex, "original.png");
            SaveTextureForDebug(_textureToProcess, "preprocessed.png");
            Destroy(finalTex);

            if (displayText != null) displayText.text = "Initializing OCR...";
            _tesseractDriver.Setup(OnTesseractSetupComplete);

            // Timeout 10 detik
            float timeout = 60f;
            float elapsed = 0f;
            while (isProcessing && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (isProcessing)
            {
                Debug.LogWarning("[NutriLens] OCR timeout! Lanjut dengan data fallback.");
                isProcessing = false;
                SetUILoading(false);
                if (displayText != null) displayText.text = "OCR Result: (timeout)";
            }
        }

        // ── Setelah OCR selesai (atau skip), panggil Analyze ─────────
        yield return new WaitForSeconds(0.5f);

        if (analyzePageManager != null)
        {
            List<float> parsedOCR = ParsingOcrOutput();
            UpdateParsedDisplay(parsedOCR);
            analyzePageManager.parsedOCR = parsedOCR;
            analyzePageManager.ShowVerticalBar();   // ← tampilkan vertical bar

            if (arVisualizationManager != null)
            arVisualizationManager.currentStatus = analyzePageManager.currentStatus;

            // Tampilkan AnalyzeButton setelah scan selesai
            if (analyzeButton != null)
                analyzeButton.gameObject.SetActive(true);
            if (snapshotButton != null)
                snapshotButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[NutriLens] AnalyzePageManager belum di-assign di Inspector!");
        }
    }

    private Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // Cek apakah Canvas menggunakan Screen Space - Camera atau Overlay
        Camera cam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;

        // Ubah koordinat ujung kiri bawah dan kanan atas ke koordinat layar
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        return new Rect(bottomLeft.x, bottomLeft.y, width, height);
    }

    private void SetUILoading(bool isLoading)
    {
        if (snapshotButton != null)
        {
            snapshotButton.interactable = !isLoading;
        }
    }

    private void OnTesseractSetupComplete()
    {
        string result = _tesseractDriver.Recognize(_textureToProcess);
        _lastScannedText = result;

        if (displayText != null)
        {
            displayText.text = "Parsing nutrition values...";
        }

        Debug.Log("OCR Result: " + result);

        if (_textureToProcess != null) Destroy(_textureToProcess);
        
        isProcessing = false;
        SetUILoading(false);
        
        Debug.Log("OCR Finished. Ready for next scan.");
    }

    private void UpdateParsedDisplay(List<float> parsedOCR)
    {
        if (displayText == null) return;

        float gula = parsedOCR != null && parsedOCR.Count > 0 ? parsedOCR[0] : 0f;
        float garam = parsedOCR != null && parsedOCR.Count > 1 ? parsedOCR[1] : 0f;
        float minyak = parsedOCR != null && parsedOCR.Count > 2 ? parsedOCR[2] : 0f;

        displayText.text =
            "Hasil Parse:\n" +
            "Gula: " + gula + " g\n" +
            "Garam: " + garam + " mg\n" +
            "Minyak: " + minyak + " g";
    }
    
    private void SaveTextureForDebug(Texture2D texture, string fileName)
    {
        byte[] pngBytes = texture.EncodeToPNG();

#if UNITY_EDITOR
        string path = System.IO.Path.Combine(Application.dataPath, "DebugCaptures", fileName);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
#else
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
#endif

        System.IO.File.WriteAllBytes(path, pngBytes);
        Debug.Log($"[DEBUG] Texture saved to: {path}");
    }

    private List<float> ParsingOcrOutput()
    {
        List<float> numbers = new List<float> { 0f, 0f, 0f };

        if (string.IsNullOrEmpty(_lastScannedText))
            return numbers;

        string[] gulaSynonyms = {
            // Indonesian
            "gula", "gula total", "gula tambahan", "gula pasir", "gula merah",
            "gula aren", "gula kelapa", "gula tebu", "gula bit", "gula halus",
            "sirup jagung", "sirup glukosa", "sirup fruktosa", "sirup maltosa",
            "sirup maple", "sirup beras", "sirup agave", "madu", "molase",
            "treacle", "dekstrosa", "glukosa", "fruktosa", "sukrosa", "laktosa",
            "maltosa", "galaktosa", "ribosa",

            // English
            "sugar", "sugars", "total sugar", "total sugars",
            "added sugar", "added sugars", "free sugars",
            "sucrose", "glucose", "fructose", "dextrose", "lactose",
            "maltose", "galactose", "ribose", "trehalose", "mannose",
            "corn syrup", "high fructose corn syrup", "hfcs",
            "glucose syrup", "fructose syrup", "malt syrup", "rice syrup",
            "agave syrup", "agave nectar", "maple syrup", "honey",
            "molasses", "brown sugar", "raw sugar", "cane sugar",
            "beet sugar", "coconut sugar", "palm sugar", "invert sugar",
            "fruit juice concentrate", "cane juice", "evaporated cane juice",
            "turbinado", "demerara", "muscovado", "panela", "rapadura",
            "icing sugar", "powdered sugar", "confectioners sugar",
        };

        string[] garamSynonyms = {
            // Indonesian
            "natrium", "garam", "garam dapur", "garam laut", "garam himalaya",
            "natrium klorida", "natrium benzoat", "natrium nitrat", "natrium nitrit",
            "natrium fosfat", "natrium sitrat", "natrium asetat", "natrium laktat",
            "natrium glutamat", "msg", "monosodium glutamat",
            "natrium bikarbonat", "soda kue", "baking soda",
            "natrium sulfat", "natrium alginat", "natrium kaseinat",
            "natrium askorbat", "garam mineral",

            // English
            "sodium", "salt", "nacl", "sea salt", "himalayan salt", "rock salt",
            "sodium chloride", "sodium benzoate", "sodium nitrate", "sodium nitrite",
            "sodium phosphate", "sodium citrate", "sodium acetate", "sodium lactate",
            "sodium glutamate", "monosodium glutamate", "msg",
            "sodium bicarbonate", "baking soda", "baking powder",
            "sodium sulfate", "sodium alginate", "sodium caseinate",
            "sodium ascorbate", "sodium erythorbate", "sodium stearoyl",
            "sodium hydroxide", "disodium", "trisodium",
        };

        string[] lemakSynonyms = {
            // Indonesian
            "lemak", "lemak total", "lemak jenuh", "lemak tak jenuh",
            "lemak tak jenuh tunggal", "lemak tak jenuh ganda",
            "lemak trans", "lemak hewani", "lemak nabati",
            "minyak", "minyak kelapa", "minyak sawit", "minyak jagung",
            "minyak kedelai", "minyak bunga matahari", "minyak kanola",
            "minyak zaitun", "minyak ikan", "minyak nabati terhidrogenasi",
            "mentega", "margarin", "shortening", "lemak babi", "gajih",
            "santan", "krim", "lemak susu", "lemak kakao",

            // English
            "fat", "fats", "total fat", "fat total",
            "saturated fat", "saturated fats", "saturated fatty acid",
            "unsaturated fat", "monounsaturated fat", "polyunsaturated fat",
            "trans fat", "trans fatty acid", "hydrogenated fat",
            "partially hydrogenated", "fully hydrogenated",
            "animal fat", "vegetable fat", "plant fat",
            "oil", "palm oil", "coconut oil", "corn oil",
            "soybean oil", "sunflower oil", "canola oil", "rapeseed oil",
            "olive oil", "fish oil", "lard", "tallow", "suet",
            "butter", "margarine", "shortening", "ghee",
            "cream", "milk fat", "dairy fat", "cocoa butter",
            "sat. fat", "sat fat", "mono fat", "poly fat",
        };

        string cleanText = _lastScannedText.ToLower();
        cleanText = System.Text.RegularExpressions.Regex.Replace(
            cleanText, @"[|\\{}\[\]@#$%^&*_=+<>]", " "
        );

        string[] lines = cleanText.Split(
            new char[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            if (TryExtractValueNearKeywords(trimmed, gulaSynonyms, out float gulaValue) &&
                gulaValue > numbers[0])
            {
                numbers[0] = gulaValue;
            }

            if (TryExtractValueNearKeywords(trimmed, garamSynonyms, out float garamValue) &&
                garamValue > numbers[1])
            {
                numbers[1] = garamValue;
            }

            if (TryExtractValueNearKeywords(trimmed, lemakSynonyms, out float lemakValue) &&
                lemakValue > numbers[2])
            {
                numbers[2] = lemakValue;
            }
        }

        Debug.Log("[NutriLens] Parsed → Gula: " + numbers[0] +
                " | Natrium: " + numbers[1] +
                " | Lemak: " + numbers[2]);

        return numbers;
    }

    private bool TryExtractValueNearKeywords(string text, string[] keywords, out float value)
    {
        value = 0f;

        foreach (string keyword in keywords)
        {
            string escapedKeyword = System.Text.RegularExpressions.Regex.Escape(keyword);

            var afterKeywordMatch = System.Text.RegularExpressions.Regex.Match(
                text,
                $@"\b{escapedKeyword}\b[^\d]{{0,24}}(\d+(?:[.,]\d+)?)"
            );

            if (TryParseMatchedNumber(afterKeywordMatch, out value))
                return true;

            var beforeKeywordMatch = System.Text.RegularExpressions.Regex.Match(
                text,
                $@"(\d+(?:[.,]\d+)?)[^\da-zA-Z]{{0,24}}\b{escapedKeyword}\b"
            );

            if (TryParseMatchedNumber(beforeKeywordMatch, out value))
                return true;
        }

        return false;
    }

    private bool TryParseMatchedNumber(System.Text.RegularExpressions.Match match, out float value)
    {
        value = 0f;

        if (!match.Success) return false;

        string numStr = match.Groups[1].Value.Replace(",", ".");

        return float.TryParse(
            numStr,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
    }
}
