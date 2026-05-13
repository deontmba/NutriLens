using TMPro;
using UnityEngine;

public class ARVisualizationManager : MonoBehaviour
{
    [Header("AR Visualization Objects")]
    [SerializeField] private GameObject arVisualizationRoot;
    [SerializeField] private GameObject mascotObject;
    [SerializeField] private GameObject donutObject;
    [SerializeField] private GameObject sugarCubeObject;

    [Header("Result UI")]
    [SerializeField] private GameObject resultContainer;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDescriptionText;

    private void Start()
    {
        HideVisualization();
    }

    public void ShowHighSugarResult()
    {
        HideAllFoodObjects();

        if (arVisualizationRoot != null)
            arVisualizationRoot.SetActive(true);

        if (resultContainer != null)
            resultContainer.SetActive(true);

        if (mascotObject != null)
            mascotObject.SetActive(true);

        if (donutObject != null)
            donutObject.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = "Tinggi gula!";

        if (resultDescriptionText != null)
            resultDescriptionText.text = "Coba pilih yang lebih rendah gula";
    }

    public void ShowHealthyResult()
    {
        HideAllFoodObjects();

        if (arVisualizationRoot != null)
            arVisualizationRoot.SetActive(true);

        if (resultContainer != null)
            resultContainer.SetActive(true);

        if (mascotObject != null)
            mascotObject.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = "Pilihan sehat!";

        if (resultDescriptionText != null)
            resultDescriptionText.text = "Kandungan nutrisinya cukup baik";
    }

    public void HideVisualization()
    {
        if (resultContainer != null)
            resultContainer.SetActive(false);

        if (arVisualizationRoot != null)
            arVisualizationRoot.SetActive(false);
    }

    private void HideAllFoodObjects()
    {
        if (donutObject != null)
            donutObject.SetActive(false);

        if (sugarCubeObject != null)
            sugarCubeObject.SetActive(false);
    }
}