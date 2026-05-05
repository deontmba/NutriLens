using UnityEngine;
using System.Collections;
using System.IO;
using Vuforia;
using UnityEngine.UI;

public class VuforiaSnapshot : MonoBehaviour
{
    [Header("Referensi Objek")]
    public Canvas uiCanvas;
    public Camera arCamera;
    public Button snapshotButton;
    private bool isProcessing = false;  
    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += InitializeVuforiaCamera;
    }

    void InitializeVuforiaCamera()
    {
        VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(PixelFormat.RGB888, true);
    }

    public void TakeAShot()
    {
        // Prevent multiple clicks
        if (isProcessing) return;
        
        StartCoroutine(CaptureAndProcess());
    }

    private IEnumerator CaptureAndProcess()
    {
        isProcessing = true;
        SetUILoading(true);
        if (uiCanvas != null) uiCanvas.enabled = false;
        yield return new WaitForEndOfFrame();

        // --- Use SCREEN dimensions as the output canvas ---
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

        // Load raw Vuforia pixels into a texture at sensor resolution
        Texture2D rawBgTex = new Texture2D(cameraImage.Width, cameraImage.Height, TextureFormat.RGB24, false);
        cameraImage.CopyToTexture(rawBgTex);
        rawBgTex.Apply();

        // Blit (scale) to screen resolution using a RenderTexture
        RenderTexture bgRT = RenderTexture.GetTemporary(outW, outH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(rawBgTex, bgRT);

        RenderTexture.active = bgRT;
        Texture2D bgTex = new Texture2D(outW, outH, TextureFormat.RGB24, false);
        bgTex.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
        bgTex.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(bgRT);
        Destroy(rawBgTex);

        // TAHAP 2: Render AR Objects at Screen Resolution
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
        Texture2D fgTex = new Texture2D(outW, outH, TextureFormat.ARGB32, false);
        fgTex.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
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

        Texture2D finalTex = new Texture2D(outW, outH);
        finalTex.SetPixels(bgPixels);
        finalTex.Apply();

        Destroy(bgTex);
        Destroy(fgTex);
        Destroy(fgRT);

        // TAHAP 4: Simpan
        byte[] bytes = finalTex.EncodeToPNG();
        Destroy(finalTex);

        string timeStamp = System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
        string fileName = "VuforiaAPI_Scan_" + timeStamp + ".png";

        string filePath;
#if UNITY_EDITOR
        filePath = Path.Combine(Application.dataPath, fileName);
#else
        filePath = Path.Combine(Application.persistentDataPath, fileName);
#endif

        File.WriteAllBytes(filePath, bytes);

        if (uiCanvas != null) uiCanvas.enabled = true;
        Debug.Log("Disimpan di:\n" + filePath);

        // --- Placeholder for OCR ---
        Debug.Log("Snapshot taken. Starting OCR simulation...");
        // We simulate OCR taking 2 seconds
        yield return new WaitForSeconds(2.0f); 

        // --- PHASE 3: Cleanup ---
        if (uiCanvas != null) uiCanvas.enabled = true;
        
        isProcessing = false;
        SetUILoading(false);
        
        Debug.Log("OCR Finished. Button is now clickable again.");
    }

    private void SetUILoading(bool isLoading)
    {
        if (snapshotButton != null)
        {
            // Disable the button so it can't be clicked
            snapshotButton.interactable = !isLoading;
        }
    }

    // TODO: instead of saving the snapshot byte, we directly process it to OCR
}