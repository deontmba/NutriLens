using UnityEngine;

/// <summary>
/// Preprocesses a Texture2D before passing to Tesseract OCR.
/// Pipeline: Grayscale → Upscale → Gaussian Blur → Adaptive Threshold
/// </summary>
public static class ImagePreprocessor
{
    // Upscale target — Tesseract works best at ~300 DPI
    // Most phone cameras give ~72-150 DPI equivalent in cropped area
    private const int TargetMinSize = 1000;

    // Adaptive threshold block size (must be odd) — how wide the local area is
    // Larger = handles bigger lighting gradient, but slower
    private const int BlockSize = 31;

    // Adaptive threshold constant — how much to subtract from local mean
    // Higher = more aggressive binarization
    private const float ThresholdC = 10f;

    // Gaussian blur radius — higher = smoother but loses detail
    private const int BlurRadius = 1;

    /// <summary>
    /// Full preprocessing pipeline. Call this on your texture before passing to Tesseract.
    /// Returns a new Texture2D — does not modify the original.
    /// </summary>
    public static Texture2D Preprocess(Texture2D source)
    {
        Texture2D grayscale = ToGrayscale(source);
        Texture2D upscaled  = Upscale(grayscale);
        Texture2D blurred   = GaussianBlur(upscaled, BlurRadius);
        Texture2D result    = AdaptiveThreshold(blurred, BlockSize, ThresholdC);

        // Cleanup intermediates
        Object.Destroy(grayscale);
        Object.Destroy(upscaled);
        Object.Destroy(blurred);

        return result;
    }

    /// <summary>
    /// Step 1 — Convert to grayscale using luminance weights.
    /// Reduces noise and simplifies subsequent steps.
    /// </summary>
    private static Texture2D ToGrayscale(Texture2D source)
    {
        int w = source.width;
        int h = source.height;
        Color32[] src = source.GetPixels32();
        Color32[] dst = new Color32[src.Length];

        for (int i = 0; i < src.Length; i++)
        {
            // Standard luminance formula (ITU-R BT.601)
            byte lum = (byte)(src[i].r * 0.299f + src[i].g * 0.587f + src[i].b * 0.114f);
            dst[i] = new Color32(lum, lum, lum, 255);
        }

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels32(dst);
        result.Apply();
        return result;
    }

    /// <summary>
    /// Step 2 — Upscale image so Tesseract has more pixels to work with.
    /// Tesseract is significantly more accurate on larger images.
    /// Uses bilinear interpolation for smooth upscaling.
    /// </summary>
    private static Texture2D Upscale(Texture2D source)
    {
        int w = source.width;
        int h = source.height;

        // Only upscale if image is too small
        if (w >= TargetMinSize && h >= TargetMinSize)
        {
            // Return a copy to keep pipeline consistent
            Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.SetPixels32(source.GetPixels32());
            copy.Apply();
            return copy;
        }

        float scale = Mathf.Max(
            (float)TargetMinSize / w,
            (float)TargetMinSize / h
        );

        int newW = Mathf.RoundToInt(w * scale);
        int newH = Mathf.RoundToInt(h * scale);

        // Use RenderTexture for GPU-accelerated bilinear scaling
        RenderTexture rt = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, rt);

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    /// <summary>
    /// Step 3 — Gaussian blur to reduce noise before thresholding.
    /// Without this, noise creates speckles in the binary image.
    /// radius=1 means a 3x3 kernel — enough to smooth without losing text detail.
    /// </summary>
    private static Texture2D GaussianBlur(Texture2D source, int radius)
    {
        int w = source.width;
        int h = source.height;
        Color32[] src = source.GetPixels32();
        Color32[] dst = new Color32[src.Length];

        // 3x3 Gaussian kernel weights (sigma ~= 1.0)
        float[] kernel = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
        float kernelSum = 16f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                int ki = 0;

                for (int ky = -radius; ky <= radius; ky++)
                {
                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        int sx = Mathf.Clamp(x + kx, 0, w - 1);
                        int sy = Mathf.Clamp(y + ky, 0, h - 1);
                        sum += src[sy * w + sx].r * kernel[ki++];
                    }
                }

                byte val = (byte)(sum / kernelSum);
                dst[y * w + x] = new Color32(val, val, val, 255);
            }
        }

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels32(dst);
        result.Apply();
        return result;
    }

    /// <summary>
    /// Step 4 — Adaptive thresholding (binarize to black and white).
    /// Unlike global threshold, this computes a LOCAL threshold per pixel
    /// based on the average of its surrounding BlockSize x BlockSize neighborhood.
    /// This handles uneven lighting and shadows extremely well.
    ///
    /// Formula: pixel = white if value > (localMean - C) else black
    /// </summary>
    private static Texture2D AdaptiveThreshold(Texture2D source, int blockSize, float C)
    {
        int w = source.width;
        int h = source.height;
        Color32[] src = source.GetPixels32();
        Color32[] dst = new Color32[src.Length];

        // Build integral image for O(1) area sum lookups
        // This makes adaptive threshold O(n) instead of O(n * blockSize^2)
        float[] integral = BuildIntegralImage(src, w, h);

        int half = blockSize / 2;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Clamp the block to image bounds
                int x1 = Mathf.Max(0, x - half);
                int y1 = Mathf.Max(0, y - half);
                int x2 = Mathf.Min(w - 1, x + half);
                int y2 = Mathf.Min(h - 1, y + half);

                float area = (x2 - x1 + 1) * (y2 - y1 + 1);
                float localSum = GetIntegralSum(integral, w, x1, y1, x2, y2);
                float localMean = localSum / area;

                // If pixel is brighter than local mean minus constant → white (background)
                // Otherwise → black (text)
                byte pixVal = src[y * w + x].r;
                byte result = pixVal > (localMean - C) ? (byte)255 : (byte)0;
                dst[y * w + x] = new Color32(result, result, result, 255);
            }
        }

        Texture2D output = new Texture2D(w, h, TextureFormat.RGBA32, false);
        output.SetPixels32(dst);
        output.Apply();
        return output;
    }

    /// <summary>
    /// Builds a summed area table (integral image) for fast area sum computation.
    /// integral[y * w + x] = sum of all pixel values in rect (0,0) to (x,y)
    /// </summary>
    private static float[] BuildIntegralImage(Color32[] src, int w, int h)
    {
        float[] integral = new float[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float val = src[y * w + x].r;

                float above = y > 0 ? integral[(y - 1) * w + x] : 0f;
                float left  = x > 0 ? integral[y * w + (x - 1)] : 0f;
                float diag  = (x > 0 && y > 0) ? integral[(y - 1) * w + (x - 1)] : 0f;

                integral[y * w + x] = val + above + left - diag;
            }
        }

        return integral;
    }

    /// <summary>
    /// Gets the sum of pixel values in rect (x1,y1)-(x2,y2) using the integral image.
    /// O(1) regardless of area size.
    /// </summary>
    private static float GetIntegralSum(float[] integral, int w, int x1, int y1, int x2, int y2)
    {
        float br = integral[y2 * w + x2];
        float tl = (x1 > 0 && y1 > 0) ? integral[(y1 - 1) * w + (x1 - 1)] : 0f;
        float tr = y1 > 0 ? integral[(y1 - 1) * w + x2] : 0f;
        float bl = x1 > 0 ? integral[y2 * w + (x1 - 1)] : 0f;

        return br - tr - bl + tl;
    }
}
