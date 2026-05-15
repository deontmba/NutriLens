using UnityEngine;
using TMPro;

// Enum global - hanya didefinisikan SEKALI di sini
public enum NutritionResult
{
    Healthy,
    HighSugar,
    HighSalt,
    HighFat,
    HighCalories
}

public class ARVisualizationManager : MonoBehaviour
{
    [Header("Demo Result")]
    public NutritionResult currentStatus;

    [Header("3D AR Objects")]
    public GameObject mascot;
    public GameObject donut;
    public GameObject salt;
    public GameObject oilDrop;
    public GameObject saladBowl;
    public GameObject sugarCube;

    [Header("UI Canvas")]
    public GameObject pindaiButton;
    public GameObject analyzeButton;
    public GameObject pindahiLagiButton;
    public GameObject scanArea;
    public TMP_Text text_tmp;
    public GameObject verticalBar;

    [Header("UI Result")]
    public GameObject resultPanel;
    public TMP_Text resultTitleText;
    public TMP_Text resultDescriptionText;

    [Header("Camera Placement")]
    public Camera arCamera;
    public float spawnDistance = 2.0f;
    public float spawnVerticalOffset = -0.5f;
    public float resultObjectHorizontalOffset = -0.4f;
    public float resultObjectVerticalOffset = 0f;

    // Simpan object hasil yang sedang aktif
    private GameObject activeResultObject = null;

    private void Start()
    {
        ResetScan();
    }

    private void LateUpdate()
    {
        // Cari AR Camera kalau belum diisi
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null) return;

        Transform cam = arCamera.transform;

        // Selalu hadapkan mascot ke kamera
        if (mascot != null && mascot.activeSelf)
            FaceCameraSmooth(mascot.transform, cam);

        // Selalu hadapkan objek hasil ke kamera
        if (activeResultObject != null && activeResultObject.activeSelf)
            FaceCameraSmooth(activeResultObject.transform, cam);
    }

    // Dipanggil saat tombol "Ketuk untuk Analisis" ditekan
    public void AnalyzeNutrition()
    {
        Debug.Log("[NutriLens] AnalyzeNutrition dipanggil. Status = " + currentStatus);

        // Sembunyikan semua objek dulu
        HideAllObjects();
        HideUICanvas();

        // Tempatkan objek di depan kamera
        PlaceObjectsInFrontOfCamera();

        // Tampilkan mascot
        if (mascot != null)
            mascot.SetActive(true);

        // Tampilkan objek + teks sesuai hasil analisis
        switch (currentStatus)
        {
            case NutritionResult.Healthy:      ShowHealthyResult();     break;
            case NutritionResult.HighSugar:    ShowHighSugarResult();   break;
            case NutritionResult.HighSalt:     ShowHighSaltResult();    break;
            case NutritionResult.HighFat:      ShowHighFatResult();     break;
            case NutritionResult.HighCalories: ShowHighCaloriesResult();break;
        }

        // Tampilkan result panel
        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    // Dipanggil saat tombol "Pindai Lagi" ditekan
    public void ResetScan()
    {
        HideAllObjects();
        activeResultObject = null;

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // Dipanggil saat balik ke halaman scan
    public void ResetUICanvasState()
    {
        HideAllObjects();
        activeResultObject = null;

        if (pindaiButton != null)   pindaiButton.SetActive(true);
        if (analyzeButton != null)  analyzeButton.SetActive(false);
        if (pindahiLagiButton != null) pindahiLagiButton.SetActive(false);
        if (scanArea != null)       scanArea.SetActive(true);
        if (text_tmp != null)       text_tmp.gameObject.SetActive(true);
        if (verticalBar != null)    verticalBar.SetActive(true);
        if (resultPanel != null)    resultPanel.SetActive(false);
    }

    // ─── Placement & FaceCamera ───────────────────────────────────────────

    private void PlaceObjectsInFrontOfCamera()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null)
        {
            Debug.LogWarning("[NutriLens] AR Camera belum diisi!");
            return;
        }

        Transform cam = arCamera.transform;

        // Posisi mascot: di depan kamera
        Vector3 mascotPos = cam.position
            + cam.forward * spawnDistance
            + cam.up * spawnVerticalOffset;

        if (mascot != null)
            mascot.transform.position = mascotPos;

        // Posisi objek hasil: di sebelah kiri mascot
        GameObject resultObject = GetResultObjectForStatus(currentStatus);
        activeResultObject = resultObject;

        if (resultObject != null)
        {
            Vector3 resultPos = mascotPos
                + cam.right * resultObjectHorizontalOffset
                + cam.up * resultObjectVerticalOffset;

            resultObject.transform.position = resultPos;
        }
    }

    private void FaceCameraSmooth(Transform target, Transform cam)
    {
        if (target == null || cam == null) return;

        Vector3 direction = cam.position - target.position;
        direction.y = 0f; // Kunci agar objek tidak tilt/miring, tetap tegak

        if (direction.magnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        target.rotation = Quaternion.Slerp(
            target.rotation,
            targetRotation,
            Time.deltaTime * 6f
        );
    }

    private GameObject GetResultObjectForStatus(NutritionResult status)
    {
        switch (status)
        {
            case NutritionResult.Healthy:      return saladBowl;
            case NutritionResult.HighSugar:    return sugarCube;
            case NutritionResult.HighSalt:     return salt;
            case NutritionResult.HighFat:      return oilDrop;
            case NutritionResult.HighCalories: return donut;
            default:                           return null;
        }
    }

    // ─── Hide/Show Helpers ────────────────────────────────────────────────

    private void HideAllObjects()
    {
        if (mascot != null)    mascot.SetActive(false);
        if (donut != null)     donut.SetActive(false);
        if (salt != null)      salt.SetActive(false);
        if (oilDrop != null)   oilDrop.SetActive(false);
        if (saladBowl != null) saladBowl.SetActive(false);
        if (sugarCube != null) sugarCube.SetActive(false);
    }

    private void HideUICanvas()
    {
        if (pindaiButton != null)      pindaiButton.SetActive(false);
        if (analyzeButton != null)     analyzeButton.SetActive(false);
        if (pindahiLagiButton != null) pindahiLagiButton.SetActive(true);
        if (scanArea != null)          scanArea.SetActive(false);
        if (text_tmp != null)          text_tmp.gameObject.SetActive(false);
        if (verticalBar != null)       verticalBar.SetActive(false);
    }

    // ─── Result Show Functions ────────────────────────────────────────────

    private void ShowHealthyResult()
    {
        if (saladBowl != null) saladBowl.SetActive(true);
        SetResultText("Sehat", "Pilihan ini terlihat lebih baik untuk tubuh.");
    }

    private void ShowHighSugarResult()
    {
        if (sugarCube != null) sugarCube.SetActive(true);
        SetResultText("Tinggi gula!", "Coba pilih produk dengan kandungan gula yang lebih rendah.");
    }

    private void ShowHighSaltResult()
    {
        if (salt != null) salt.SetActive(true);
        SetResultText("Tinggi garam!", "Produk ini memiliki kandungan sodium/garam yang perlu dibatasi.");
    }

    private void ShowHighFatResult()
    {
        if (oilDrop != null) oilDrop.SetActive(true);
        SetResultText("Tinggi lemak!", "Perhatikan kandungan lemak, terutama lemak jenuh.");
    }

    private void ShowHighCaloriesResult()
    {
        if (donut != null) donut.SetActive(true);
        SetResultText("Tinggi kalori!", "Konsumsi secukupnya agar tidak berlebihan.");
    }

    private void SetResultText(string title, string description)
    {
        if (resultTitleText != null)       resultTitleText.text = title;
        if (resultDescriptionText != null) resultDescriptionText.text = description;
    }
}