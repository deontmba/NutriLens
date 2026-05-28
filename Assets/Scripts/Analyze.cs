using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Analyze : MonoBehaviour
{
    public List<float> parsedOCR;
    public NutritionResult currentStatus { get; private set; }
    public GameObject verticalBarObject;
    public RectTransform pointerRect;
    public ARVisualizationManager aRVisualizationManager;

    private float pointerPosSehat = -150f;
    private float pointerPosSedang = 0f;
    private float pointerPosTidakSehat = 150f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // TODO (Gideon): Process the parsedOCR data (change the function type as needed)
    private NutritionResult AnalyzeData()
    {
        if (parsedOCR == null || parsedOCR.Count < 3)
            parsedOCR = new List<float> { 0f, 0f, 0f };

        float gula   = parsedOCR[0]; // gram
        float natrium = parsedOCR[1]; // mg
        float lemak  = parsedOCR[2]; // gram

        // Cek masing-masing apakah masuk zona merah
        bool gulaRed   = gula   > 10f;
        bool natriumRed = natrium > 400f;
        bool lemakRed  = lemak  > 4f;

        // Kalori estimasi sederhana
        // Kalori dari gula: 4 kcal/g, lemak: 9 kcal/g
        float estimasiKalori = (gula * 4f) + (lemak * 9f);
        bool kaloriTinggi = estimasiKalori > 200f;

        // Prioritas: cek nutrisi mana yang paling dominan buruk
        // Kalau semuanya aman → Healthy
        if (!gulaRed && !natriumRed && !lemakRed && !kaloriTinggi)
            return NutritionResult.Healthy;

        // Cari yang paling tinggi relatif terhadap threshold-nya
        float gulaRatio   = gula    / 10f;
        float natriumRatio = natrium / 400f;
        float lemakRatio  = lemak   / 4f;
        float kaloriRatio = estimasiKalori / 200f;

        // Return berdasarkan ratio tertinggi
        float maxRatio = Mathf.Max(gulaRatio, natriumRatio, lemakRatio, kaloriRatio);

        if (maxRatio == kaloriRatio && kaloriTinggi)
            return NutritionResult.HighCalories;
        else if (maxRatio == gulaRatio && gulaRed)
            return NutritionResult.HighSugar;
        else if (maxRatio == natriumRatio && natriumRed)
            return NutritionResult.HighSalt;
        else if (maxRatio == lemakRatio && lemakRed)
            return NutritionResult.HighFat;
        else
            return NutritionResult.Healthy;

        // The Range for gula, garam, and lemak was written in our Report's Core Mechanic Section 
        // (https://docs.google.com/document/d/1XChqsG0PHlAG1cEluaOARZ-S-31r37AsXN7Ufp355MI/edit?usp=sharing)
    }

    public void ShowVerticalBar()
    {
        // 1. Get the data first (Save analyzed data output on this class Variable so other function can access it)
        currentStatus = AnalyzeData();

    // Kirim hasil ke ARVisualizationManager
    if (aRVisualizationManager != null)
        aRVisualizationManager.currentStatus = currentStatus;

    if (verticalBarObject != null)
        verticalBarObject.SetActive(true);

    float targetY = pointerPosSedang;
    if (currentStatus == NutritionResult.Healthy)
        targetY = pointerPosSehat;
    else if (currentStatus == NutritionResult.HighSugar ||
             currentStatus == NutritionResult.HighSalt  ||
             currentStatus == NutritionResult.HighFat   ||
             currentStatus == NutritionResult.HighCalories)
        targetY = pointerPosTidakSehat;

    if (pointerRect != null)
        StartCoroutine(MovePointer(targetY));

    Debug.Log("[NutriLens] Status: " + currentStatus +
              " | Gula: " + parsedOCR[0] + "g" +
              " | Natrium: " + parsedOCR[1] + "mg" +
              " | Lemak: " + parsedOCR[2] + "g");
    }

    // Animasi pointer bergerak smooth ke posisi target (kalo perlu ya)
    private IEnumerator MovePointer(float targetY)
    {
        float duration = 0.8f;
        float elapsed  = 0f;
        Vector2 startPos = pointerRect.anchoredPosition;
        Vector2 endPos   = new Vector2(startPos.x, targetY);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            pointerRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        pointerRect.anchoredPosition = endPos;
    }
}
