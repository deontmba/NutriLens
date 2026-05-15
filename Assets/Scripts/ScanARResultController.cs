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

    [Header("Camera Placement")]
    public Camera arCamera;
    public float spawnDistance = 1.2f;
    public float spawnVerticalOffset = -0.25f;
    public float resultObjectHorizontalOffset = -0.35f;
    public float resultObjectVerticalOffset = 0.25f;

    private void Start()
    {
        ResetScan();
    }

    public void AnalyzeNutrition()
    {
        Debug.Log("AnalyzeNutrition dipanggil. Current Result = " + currentResult);

        HideAllObjects();

        PlaceObjectsInFrontOfCamera();

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

        if (resultTitleText != null)
        {
            resultTitleText.text = "";
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = "";
        }
    }

    private void LateUpdate()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null) return;

        Transform cam = arCamera.transform;

        if (mascot != null && mascot.activeSelf)
        {
            FaceCameraSmooth(mascot.transform, cam);
        }

        GameObject resultObject = GetCurrentResultObject();
        if (resultObject != null && resultObject.activeSelf)
        {
            FaceCameraSmooth(resultObject.transform, cam);
        }
    }

    // Ganti fungsi FaceCamera yang lama dengan ini
    private void FaceCameraSmooth(Transform target, Transform cam)
    {
        if (target == null || cam == null) return;

        Vector3 direction = cam.position - target.position;

        if (direction != Vector3.zero)
        {
            // Gunakan cam.up agar objek mengikuti orientasi kamera sepenuhnya
            // termasuk saat kamera miring ke atas atau bawah
            Quaternion targetRotation = Quaternion.LookRotation(direction, cam.up);
            target.rotation = Quaternion.Slerp(
                target.rotation, 
                targetRotation, 
                Time.deltaTime * 8f
            );
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

    private void PlaceObjectsInFrontOfCamera()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            Debug.LogWarning("AR Camera belum diisi di ScanARResultController.");
            return;
        }

        Transform cam = arCamera.transform;

        Vector3 mascotPosition =
            cam.position +
            cam.forward * spawnDistance +
            cam.up * spawnVerticalOffset;

        GameObject resultObject = GetCurrentResultObject();

        if (mascot != null)
        {
            mascot.transform.position = mascotPosition;
            FaceCameraSmooth(mascot.transform, cam);
        }

        if (resultObject != null)
        {
            Vector3 resultObjectPosition =
                mascotPosition +
                cam.right * resultObjectHorizontalOffset +
                cam.up * resultObjectVerticalOffset;

            resultObject.transform.position = resultObjectPosition;
            FaceCameraSmooth(resultObject.transform, cam);
        }
    }

    private GameObject GetCurrentResultObject()
    {
        switch (currentResult)
        {
            case NutritionResult.Healthy:
                return saladBowl;

            case NutritionResult.HighSugar:
                return sugarCube;

            case NutritionResult.HighSalt:
                return salt;

            case NutritionResult.HighFat:
                return oilDrop;

            case NutritionResult.HighCalories:
                return donut;

            default:
                return null;
        }
    }

    private void ShowHealthyResult()
    {
        if (saladBowl != null) saladBowl.SetActive(true);

        SetResultText(
            "Pilihan sehat",
            "Produk ini memiliki kandungan nutrisi yang lebih baik untuk dikonsumsi."
        );
    }

    private void ShowHighSugarResult()
    {
        if (sugarCube != null) sugarCube.SetActive(true);

        SetResultText(
            "Tinggi gula",
            "Coba pilih produk dengan kandungan gula yang lebih rendah."
        );
    }

    private void ShowHighSaltResult()
    {
        if (salt != null) salt.SetActive(true);

        SetResultText(
            "Tinggi garam",
            "Produk ini memiliki kandungan sodium atau garam yang perlu dibatasi."
        );
    }

    private void ShowHighFatResult()
    {
        if (oilDrop != null) oilDrop.SetActive(true);

        SetResultText(
            "Tinggi lemak",
            "Perhatikan kandungan lemak, terutama lemak jenuh."
        );
    }

    private void ShowHighCaloriesResult()
    {
        if (donut != null) donut.SetActive(true);

        SetResultText(
            "Tinggi kalori",
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