using UnityEngine;
using System.Collections.Generic;

public class Analyze : MonoBehaviour
{
    public List<float> parsedOCR;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // TODO (Gideon): Process the parsedOCR data (change the function type as needed)
    private void AnalyzeData()
    {
        // This is only mock data, since we haven't implemented the OCR parsing (modify the value as you want for testing)
        parsedOCR = new List<float> {8f, 7.5f, 10f};
        
        /* NOTE:         
         * List[0] -> Gula
         * List[1] -> Garam
         * List[2] -> Lemak
        */
        
        // The Range for gula, garam, and lemak was written in our Report's Core Mechanic Section 
        // (https://docs.google.com/document/d/1XChqsG0PHlAG1cEluaOARZ-S-31r37AsXN7Ufp355MI/edit?usp=sharing)
    }

    // TODO (Gideon): Show the vertical bar on the ScanPage Scene (check mockup for detail)
    // Make sure there is animation on the vertical bar pointer (either moving up or down)
    // NOTE: Make sure this function TRIGGERS the vertical bar to appear in the scene
    public void ShowVerticalBar()
    {
        // 1. Get the data first
        AnalyzeData(); // Change the function type as needed since AnalyzeData should return something so that you can process on this function

        // 2. Add your UI trigger and animation logic here
    }

    // TODO (Rantizi): Trigger the AR 3D object visualization. 
    // Make sure to discuss with Gideon to ensure you receive the correct data values needed from the analysis.
    public void TriggerARAnimation()
    {
        
    }
}
