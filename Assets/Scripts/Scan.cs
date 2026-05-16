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
            float timeout = 10f;
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
            analyzePageManager.parsedOCR = parsedOCR;
            analyzePageManager.ShowVerticalBar();   // ← tampilkan vertical bar

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

        
        if (displayText != null)
        {
            displayText.text = "OCR Result:\n" + result;
           _lastScannedText = result;
        }

        Debug.Log("OCR Result: " + result);

        if (_textureToProcess != null) Destroy(_textureToProcess);
        
        isProcessing = false;
        SetUILoading(false);
        
        Debug.Log("OCR Finished. Ready for next scan.");
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
        List<float> numbers = new();
        /*
         * TODO (Gideon): Parsing OCR Output
         * List[0] -> Gula
         * List[1] -> Garam
         * List[2] -> Lemak
         * Fallback -> all 0.f
        */
        // _lastScannedText

        return numbers;
    }
}