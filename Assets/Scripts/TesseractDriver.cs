using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class TesseractDriver
{
    private TesseractWrapper _tesseract;
    private static readonly List<string> fileNames = new List<string> {"tessdata.tgz"};

    public string CheckTessVersion()
    {
        _tesseract = new TesseractWrapper();

        try
        {
            string version = "Tesseract version: " + _tesseract.Version();
            Debug.Log(version);
            return version;
        }
        catch (Exception e)
        {
            string errorMessage = e.GetType() + " - " + e.Message;
            Debug.LogError("Tesseract version: " + errorMessage);
            return errorMessage;
        }
    }

    public void Setup(UnityAction onSetupComplete)
    {
#if UNITY_EDITOR
        OcrSetup(onSetupComplete);
#elif UNITY_ANDROID
        CopyAllFilesToPersistentData(fileNames, onSetupComplete);
#else
        OcrSetup(onSetupComplete);
#endif
    }

    public void OcrSetup(UnityAction onSetupComplete)
    {
        Debug.Log("[OCR] OcrSetup called");
        _tesseract = new TesseractWrapper();

#if UNITY_EDITOR
        string datapath = Path.Combine(Application.streamingAssetsPath, "tessdata");
#elif UNITY_ANDROID
        string datapath = Application.persistentDataPath + "/tessdata/";
#else
        string datapath = Path.Combine(Application.streamingAssetsPath, "tessdata");
#endif

        if (_tesseract.Init("eng+ind", datapath))
        {
            Debug.Log("Init Successful");
            onSetupComplete?.Invoke();
        }
        else
        {
            Debug.LogError(_tesseract.GetErrorMessage());
            Debug.LogError("[OCR] Init FAILED: " + _tesseract.GetErrorMessage());
        }
    }

    private async void CopyAllFilesToPersistentData(List<string> fileNames, UnityAction onSetupComplete)
    {
        try
        {
            string fromPath = "jar:file://" + Application.dataPath + "!/assets/";
            string toPath = Application.persistentDataPath + "/";

            foreach (string fileName in fileNames)
            {
                if (!File.Exists(toPath + fileName))
                {
                    Debug.Log("[OCR] Copying: " + fromPath + fileName);

                    using (UnityWebRequest www = UnityWebRequest.Get(fromPath + fileName))
                    {
                        var operation = www.SendWebRequest();
                        while (!operation.isDone)
                            await Task.Yield();

                        if (www.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError("[OCR] Copy FAILED: " + www.error);
                            return;
                        }

                        File.WriteAllBytes(toPath + fileName, www.downloadHandler.data);
                        Debug.Log("[OCR] Copy done: " + toPath + fileName);
                    }

                    UnZipData(fileName);
                }
                else
                {
                    Debug.Log("[OCR] File already exists: " + toPath + fileName);
                }
            }

            Debug.Log("[OCR] Calling OcrSetup...");
            OcrSetup(onSetupComplete);
        }
        catch (Exception e)
        {
            // This is what was silently killing your flow before
            Debug.LogError("[OCR] CopyAllFilesToPersistentData EXCEPTION: " + e.Message + "\n" + e.StackTrace);
        }
    }

    public string GetErrorMessage()
    {
        return _tesseract?.GetErrorMessage();
    }

    public string Recognize(Texture2D imageToRecognize)
    {
        return _tesseract.Recognize(imageToRecognize);
    }

    public Texture2D GetHighlightedTexture()
    {
        return _tesseract.GetHighlightedTexture();
    }

    private void UnZipData(string fileName)
    {
        if (File.Exists(Application.persistentDataPath + "/" + fileName))
        {
            UnZipUtil.ExtractTGZ(Application.persistentDataPath + "/" + fileName, Application.persistentDataPath);
            Debug.Log("UnZipping Done");
        }
        else
        {
            Debug.LogError(fileName + " not found!");
        }
    }
}