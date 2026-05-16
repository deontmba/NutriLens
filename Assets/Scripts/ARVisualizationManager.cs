using UnityEngine;
using TMPro;

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

    [Header("3D AR Objects (di dalam ARContentRoot)")]
    public GameObject mascot;
    public GameObject donut;
    public GameObject salt;
    public GameObject oilDrop;
    public GameObject saladBowl;
    public GameObject sugarCube;

    [Header("Animasi Mascot")]
    public Animator mascotAnimator;

    [Header("Audio")]
    public AudioSource mascotAudioSource;
    public AudioClip soundMascotMuncul;
    public AudioClip soundMascotHappy;
    public AudioClip soundMascotSedih;

    [Header("Bubble Chat 3D (World Space Canvas di Mascot)")]
    public GameObject bubbleRoot;
    public TMP_Text bubbleTitleText;
    public TMP_Text bubbleDescriptionText;

    [Header("UI Layar (Screen Space)")]
    public GameObject pindaiButton;
    public GameObject analyzeButton;
    public GameObject pindahiLagiButton;
    public GameObject scanArea;
    public TMP_Text text_tmp;
    public GameObject verticalBar;

    [Header("Camera")]
    public Camera arCamera;

    private void Start()
    {
        ResetScan();
    }

    // ─── Public Functions (dipanggil dari Button) ─────────────────

    // Dipanggil saat tombol "Ketuk untuk Analisis" ditekan
    public void AnalyzeNutrition()
    {
        Debug.Log("[NutriLens] AnalyzeNutrition. Status = " + currentStatus);

        HideAllObjects();
        HideUICanvas();

        // Tampilkan mascot + suara muncul
        if (mascot != null)
        {
            mascot.SetActive(true);
            PlaySound(soundMascotMuncul);
        }

        // Tampilkan objek + animasi + suara + bubble sesuai hasil
        switch (currentStatus)
        {
            case NutritionResult.Healthy:      ShowHealthyResult();      break;
            case NutritionResult.HighSugar:    ShowHighSugarResult();    break;
            case NutritionResult.HighSalt:     ShowHighSaltResult();     break;
            case NutritionResult.HighFat:      ShowHighFatResult();      break;
            case NutritionResult.HighCalories: ShowHighCaloriesResult(); break;
        }

        // Tampilkan bubble chat
        if (bubbleRoot != null)
            bubbleRoot.SetActive(true);
    }

    // Dipanggil saat tombol "Pindai Lagi" ditekan
    public void ResetUICanvasState()
    {
        HideAllObjects();

        if (pindaiButton != null)      pindaiButton.SetActive(true);
        if (analyzeButton != null)     analyzeButton.SetActive(false);
        if (pindahiLagiButton != null) pindahiLagiButton.SetActive(false);
        if (scanArea != null)          scanArea.SetActive(true);
        if (text_tmp != null)          text_tmp.gameObject.SetActive(true);
        if (verticalBar != null)       verticalBar.SetActive(true);

        // Reset animasi mascot ke idle
        PlayMascotAnimation(0);
    }

    // Dipanggil saat Start() untuk reset ke kondisi awal
    public void ResetScan()
    {
        HideAllObjects();

        if (pindaiButton != null)      pindaiButton.SetActive(true);
        if (analyzeButton != null)     analyzeButton.SetActive(false);
        if (pindahiLagiButton != null) pindahiLagiButton.SetActive(false);
        if (scanArea != null)          scanArea.SetActive(true);
        if (text_tmp != null)          text_tmp.gameObject.SetActive(true);
        if (verticalBar != null)       verticalBar.SetActive(true);
    }

    // ─── Animasi Mascot ───────────────────────────────────────────

    private void PlayMascotAnimation(int emotionState)
    {
        // 0 = Idle, 1 = Senang (Healthy), 2 = Lesu (tidak sehat)
        if (mascotAnimator == null)
        {
            Debug.LogWarning("[NutriLens] mascotAnimator belum di-assign di Inspector!");
            return;
        }

        mascotAnimator.SetInteger("emotionState", emotionState);
        Debug.Log("[NutriLens] Mascot animasi emotionState = " + emotionState);
    }

    // ─── Audio ────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (mascotAudioSource == null)
        {
            Debug.LogWarning("[NutriLens] mascotAudioSource belum di-assign di Inspector!");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[NutriLens] AudioClip null, skip play sound.");
            return;
        }

        mascotAudioSource.PlayOneShot(clip);
    }

    // ─── Hide/Show Helpers ────────────────────────────────────────

    private void HideAllObjects()
    {
        if (mascot != null)     mascot.SetActive(false);
        if (donut != null)      donut.SetActive(false);
        if (salt != null)       salt.SetActive(false);
        if (oilDrop != null)    oilDrop.SetActive(false);
        if (saladBowl != null)  saladBowl.SetActive(false);
        if (sugarCube != null)  sugarCube.SetActive(false);
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
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

    // ─── Result Show Functions ────────────────────────────────────

    private void ShowHealthyResult()
    {
        if (saladBowl != null) saladBowl.SetActive(true);
        PlayMascotAnimation(1);          // Senang
        PlaySound(soundMascotHappy);     // Suara happy
        SetBubbleText(
            "Sehat",
            "Pilihan ini terlihat lebih baik untuk tubuhmu!"
        );
    }

    private void ShowHighSugarResult()
    {
        if (sugarCube != null) sugarCube.SetActive(true);
        PlayMascotAnimation(2);          // Lesu
        PlaySound(soundMascotSedih);     // Suara sedih
        SetBubbleText(
            "Tinggi Gula!",
            "Hati-hati! Kandungan gula produk ini cukup tinggi. Batasi konsumsinya ya!"
        );
    }

    private void ShowHighSaltResult()
    {
        if (salt != null) salt.SetActive(true);
        PlayMascotAnimation(2);          // Lesu
        PlaySound(soundMascotSedih);     // Suara sedih
        SetBubbleText(
            "Tinggi Garam!",
            "Sodium-nya tinggi nih! Terlalu banyak garam tidak baik untuk tekanan darah."
        );
    }

    private void ShowHighFatResult()
    {
        if (oilDrop != null) oilDrop.SetActive(true);
        PlayMascotAnimation(2);          // Lesu
        PlaySound(soundMascotSedih);     // Suara sedih
        SetBubbleText(
            "Tinggi Lemak!",
            "Lemak jenuhnya tinggi! Konsumsi berlebihan bisa meningkatkan kolesterol."
        );
    }

    private void ShowHighCaloriesResult()
    {
        if (donut != null) donut.SetActive(true);
        PlayMascotAnimation(2);          // Lesu
        PlaySound(soundMascotSedih);     // Suara sedih
        SetBubbleText(
            "Tinggi Kalori!",
            "Kalorinya banyak banget! Pastikan sesuai dengan kebutuhan harianmu."
        );
    }

    // ─── Bubble Text Helper ───────────────────────────────────────

    private void SetBubbleText(string title, string description)
    {
        if (bubbleTitleText != null)
            bubbleTitleText.text = title;
        if (bubbleDescriptionText != null)
            bubbleDescriptionText.text = description;
    }
}