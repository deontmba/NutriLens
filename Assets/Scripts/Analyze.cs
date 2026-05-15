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
        // This is only mock data, since we haven't implemented the OCR parsing (modify the value as you want for testing)
        parsedOCR = new List<float> {8000f, 250f, 3f};

        int redCount    = 0;
        int yellowCount = 0;
        
        /* NOTE:         
         * List[0] -> Gula
         * List[1] -> Garam
         * List[2] -> Lemak
        */

        // - Gula (batas harian: 50g)
        // Hijau: ≤ 5g | Kuning: 5–10g | Merah: > 10g
        if (parsedOCR[0] > 10f)      redCount++;
        else if (parsedOCR[0] > 5f)  yellowCount++;

        // - Garam/Natrium (batas harian: 2000mg)
        // Hijau: ≤ 200mg | Kuning: 200–400mg | Merah: > 400mg
        if (parsedOCR[1] > 400f)      redCount++;
        else if (parsedOCR[1] > 200f) yellowCount++;

        // - Lemak Jenuh (batas harian: 22g)
        // Hijau: ≤ 2g | Kuning: 2–4g | Merah: > 4g
        if (parsedOCR[2] > 4f)      redCount++;
        else if (parsedOCR[2] > 2f) yellowCount++;

        // Status dominan
        if (redCount >= 1)                        
            return NutritionResult.Healthy;
        else if (yellowCount >= 2) 
            return NutritionResult.Healthy;
        else                                       
        return NutritionResult.Healthy;

        // The Range for gula, garam, and lemak was written in our Report's Core Mechanic Section 
        // (https://docs.google.com/document/d/1XChqsG0PHlAG1cEluaOARZ-S-31r37AsXN7Ufp355MI/edit?usp=sharing)
    }

    public void ShowVerticalBar()
    {
        // 1. Get the data first (Save analyzed data output on this class Variable so other function can access it)
        currentStatus = AnalyzeData(); 
        aRVisualizationManager.currentStatus = currentStatus;

        // 2. Add your UI trigger and animation logic here
        if (verticalBarObject != null)
            verticalBarObject.SetActive(true);

        // Tentukan target posisi pointer berdasarkan status gizi
        float targetY = pointerPosSedang;
        if (currentStatus == NutritionResult.Healthy)
            targetY = pointerPosSehat;
        else if (currentStatus == NutritionResult.Healthy)
            targetY = pointerPosTidakSehat;

        // Animasi pointer bergerak ke posisi target
        if (pointerRect != null)
            StartCoroutine(MovePointer(targetY));

        // Log untuk debugging
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
