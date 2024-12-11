using UnityEngine;

public class PerlinNoise : MonoBehaviour
{
    /// <summary>
    /// Generates a 2D noise map using Perlin noise with multiple octaves.
    /// </summary>
    /// <param name="width">The width of the map.</param>
    /// <param name="height">The height of the map.</param>
    /// <param name="scale">Base scale of the noise (larger values = more "stretched out" noise).</param>
    /// <param name="octaves">Number of noise layers to blend together.</param>
    /// <param name="persistence">How quickly amplitude decreases with each octave. Typical range: (0,1).</param>
    /// <param name="lacunarity">How quickly frequency increases with each octave. Typical range: >1.</param>
    /// <param name="offsetX">Horizontal offset of the noise. Useful for shifting the map around.</param>
    /// <param name="offsetY">Vertical offset of the noise.</param>
    /// <returns>A 2D array of floats representing the noise values [0,1].</returns>
    public static float[,] GenerateNoiseMap(
        int width,
        int height,
        float scale,
        int octaves,
        float persistence,
        float lacunarity,
        int seed)
    {
        using(new RandomState(seed))
        {
            return GenerateNoiseMapSeeded(width, height, scale, octaves, persistence, lacunarity, seed);
        }
    }

    private static float[,] GenerateNoiseMapSeeded(
        int width,
        int height,
        float scale,
        int octaves,
        float persistence,
        float lacunarity,
        int seed)
    {
        float[,] noiseMap = new float[width, height];

        //Generate a list of offsets for each octave
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = Random.Range(-100000, 100000);
            float offsetY = Random.Range(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Prevent division by zero
        if (scale <= 0f)
            scale = 0.0001f;

        // Track min and max values for normalization
        float minNoiseValue = float.MaxValue;
        float maxNoiseValue = float.MinValue;

        // For each coordinate in the noise map:
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                // Generate noise for each octave
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y + octaveOffsets[i].y) / scale * frequency;

                    // PerlinNoise returns a value between 0 and 1, shift it to range [-1, 1]
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

                    // Accumulate noise
                    noiseHeight += perlinValue * amplitude;

                    // Update amplitude and frequency for next octave
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Update min and max noise values
                if (noiseHeight > maxNoiseValue) maxNoiseValue = noiseHeight;
                if (noiseHeight < minNoiseValue) minNoiseValue = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // Normalize the noise map to 0 - 1
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalize by squeezing values between 0 and 1
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}
