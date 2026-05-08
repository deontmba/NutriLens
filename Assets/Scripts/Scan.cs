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
        if (uiCanvas != null) uiCanvas.enabled = false;
        yield return new WaitForEndOfFrame();

        // 1. Dapatkan posisi layar dari scanArea (Raw Image)
        Rect screenRect = GetScreenRect(scanArea);

        // Clamping untuk memastikan area tidak keluar dari batas layar
        int startX = Mathf.Clamp(Mathf.RoundToInt(screenRect.x), 0, Screen.width);
        int startY = Mathf.Clamp(Mathf.RoundToInt(screenRect.y), 0, Screen.height);
        int cropW = Mathf.Clamp(Mathf.RoundToInt(screenRect.width), 1, Screen.width - startX);
        int cropH = Mathf.Clamp(Mathf.RoundToInt(screenRect.height), 1, Screen.height - startY);

        int outW = Screen.width;
        int outH = Screen.height;

        // TAHAP 1: Ambil Gambar Vuforia & Scale ke Screen Size
        Vuforia.Image cameraImage = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.RGB888);

        if (cameraImage == null)
        {
            Debug.LogError("GAGAL: Vuforia camera image null.");
            if (uiCanvas != null) uiCanvas.enabled = true;
            yield break;
        }

        Texture2D rawBgTex = new Texture2D(cameraImage.Width, cameraImage.Height, TextureFormat.RGB24, false);
        cameraImage.CopyToTexture(rawBgTex);
        rawBgTex.Apply();

        RenderTexture bgRT = RenderTexture.GetTemporary(outW, outH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(rawBgTex, bgRT);

        RenderTexture.active = bgRT;
        // BIKIN TEXTURE SESUAI UKURAN CROP SAJA
        Texture2D bgTex = new Texture2D(cropW, cropH, TextureFormat.RGB24, false);
        // READ PIXELS HANYA DI AREA CROP
        bgTex.ReadPixels(new Rect(startX, startY, cropW, cropH), 0, 0); 
        bgTex.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(bgRT);
        Destroy(rawBgTex);

        // TAHAP 2: Render  at Screen Resolution
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
        // BIKIN TEXTURE SESUAI UKURAN CROP SAJA
        Texture2D fgTex = new Texture2D(cropW, cropH, TextureFormat.ARGB32, false);
        // READ PIXELS HANYA DI AREA CROP
        fgTex.ReadPixels(new Rect(startX, startY, cropW, cropH), 0, 0);
        fgTex.Apply();
        RenderTexture.active = null;

        // TAHAP 3: Compositing
        Color[] bgPixels = bgTex.GetPixels();
        Color[] fgPixels = fgTex.GetPixels();

        for (int i = 0; i < bgPixels.Length; i++)
        {
            Color fg = fgPixels[i];
            bgPixels[i] = fg * fg.a + bgPixels[i] * (1f - fg.a);
        }

        // BIKIN FINAL TEXTURE SESUAI UKURAN CROP
        Texture2D finalTex = new Texture2D(cropW, cropH);
        finalTex.SetPixels(bgPixels);
        finalTex.Apply();

        Destroy(bgTex);
        Destroy(fgTex);
        Destroy(fgRT);

        _textureToProcess = finalTex;

        Debug.Log($"Snapshot taken at {cropW}x{cropH}. Starting OCR simulation...");
        if (displayText != null) displayText.text = "Initializing OCR...";
        
        _tesseractDriver.Setup(OnTesseractSetupComplete);

        while (isProcessing)
        {
            yield return null;
        }

        if (analyzePageManager != null)
        {
            List<float> parsedOCR = ParsingOcrOutput();
            // Analyze and Show Vertical Bar
            analyzePageManager.parsedOCR = parsedOCR;
            analyzePageManager.ShowVerticalBar();
        }
        else
        {
            Debug.LogError("AnalyzePageManager is not assigned in the Inspector!");
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
        
        if (uiCanvas != null) uiCanvas.enabled = true;
        isProcessing = false;
        SetUILoading(false);
        
        Debug.Log("OCR Finished. Ready for next scan.");
    }
    
    private List<float> ParsingOcrOutput()
    {
        List<float> numbers = new();
        /*
         * TODO (Wilson): Parsing OCR Output
         * List[0] -> Gula
         * List[1] -> Garam
         * List[2] -> Lemak
         * Fallback -> all 0.f
        */
        // _lastScannedText

        return numbers;
    }
}