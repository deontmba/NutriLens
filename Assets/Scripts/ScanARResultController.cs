using UnityEngine;
using TMPro;

public class ScanARResultController : MonoBehaviour
{
    public enum NutritionResult
    {
        Healthy,
        HighSugar,
        HighSalt,
        HighFat,
        HighCalories
    }

    [Header("Demo Result")]
    public NutritionResult currentResult = NutritionResult.HighSugar;

    [Header("3D AR Objects")]
    public GameObject mascot;
    public GameObject donut;
    public GameObject salt;
    public GameObject oilDrop;
    public GameObject saladBowl;
    public GameObject sugarCube;

    [Header("UI Result")]
    public GameObject resultPanel;
    public TMP_Text resultTitleText;
    public TMP_Text resultDescriptionText;

    private void Start()
    {
        ResetScan();
    }

    public void AnalyzeNutrition()
    {
        Debug.Log("AnalyzeNutrition dipanggil. Current Result = " + currentResult);

        HideAllObjects();

        if (mascot != null)
        {
            mascot.SetActive(true);
        }

        switch (currentResult)
        {
            case NutritionResult.Healthy:
                ShowHealthyResult();
                break;

            case NutritionResult.HighSugar:
                ShowHighSugarResult();
                break;

            case NutritionResult.HighSalt:
                ShowHighSaltResult();
                break;

            case NutritionResult.HighFat:
                ShowHighFatResult();
                break;

            case NutritionResult.HighCalories:
                ShowHighCaloriesResult();
                break;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }

    public void ResetScan()
    {
        HideAllObjects();

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void HideAllObjects()
    {
        if (mascot != null) mascot.SetActive(false);
        if (donut != null) donut.SetActive(false);
        if (salt != null) salt.SetActive(false);
        if (oilDrop != null) oilDrop.SetActive(false);
        if (saladBowl != null) saladBowl.SetActive(false);
        if (sugarCube != null) sugarCube.SetActive(false);
    }

    private void ShowHealthyResult()
    {
        if (saladBowl != null) saladBowl.SetActive(true);

        SetResultText(
            "Sehat",
            "Pilihan ini terlihat lebih baik untuk tubuh."
        );
    }

    private void ShowHighSugarResult()
    {
        if (sugarCube != null) sugarCube.SetActive(true);

        SetResultText(
            "Tinggi gula ⚠️",
            "Coba pilih produk dengan kandungan gula yang lebih rendah."
        );
    }

    private void ShowHighSaltResult()
    {
        if (salt != null) salt.SetActive(true);

        SetResultText(
            "Tinggi garam ⚠️",
            "Produk ini memiliki kandungan sodium/garam yang perlu dibatasi."
        );
    }

    private void ShowHighFatResult()
    {
        if (oilDrop != null) oilDrop.SetActive(true);

        SetResultText(
            "Tinggi lemak ⚠️",
            "Perhatikan kandungan lemak, terutama lemak jenuh."
        );
    }

    private void ShowHighCaloriesResult()
    {
        if (donut != null) donut.SetActive(true);

        SetResultText(
            "Tinggi kalori ⚠️",
            "Konsumsi secukupnya agar tidak berlebihan."
        );
    }

    private void SetResultText(string title, string description)
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = title;
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = description;
        }
    }
}