using PixelsForGlory.VoronoiDiagram;
using System.Collections.Generic;
using System.Linq;
using TriangleNet;
using TriangleNet.Geometry;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{

    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private Material baseMaterial;

    private MeshRenderer meshRenderer;

    [SerializeField] private float scale;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int octaves;
    [SerializeField] private float lacunarity;
    [SerializeField] private float persistence;
    [SerializeField] private int seed;
    [SerializeField] private int numberOfTerritories;

    [SerializeField] private GameObject temp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = baseMaterial;

        scale = (float)DoubleParameters.DefaultWorldScale.GetValue();
        width = (int)DoubleParameters.DefaultWorldWidth.GetValue();
        height = (int)DoubleParameters.DefaultWorldHeight.GetValue();
        octaves = (int)DoubleParameters.DefaultWorldOctaves.GetValue();
        lacunarity = (float)DoubleParameters.DefaultWorldLacunarity.GetValue();
        persistence = (float)DoubleParameters.DefaultWorldPersistence.GetValue();

        GenerateWorld();

        //Experimenting
        //Plane is 11 vertices by 11 vertices
        float[,] noiseMap = PerlinNoise.GenerateNoiseMap(11, 11, 0.4f, octaves, lacunarity, persistence, seed);

        Texture2D texture = new Texture2D(11, 11);
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 11; y++)
            {

                var noiseValue = noiseMap[x, y];

                //These need to add up to 1
                float r = noiseValue > 0.5f ? 1 : 0;
                float g = noiseValue > 0.5f ? 0 : 1;
                float b = 0;
                float a = 0;

                Vector4 vector4 = new Vector4(r, g, b, a).normalized;
                var color = new Color(vector4.x, vector4.y, vector4.z, vector4.w);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        //temp.GetComponent<MeshRenderer>().material.SetTexture("Splat Map", texture);
        temp.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }

    private Texture2D GenerateSplatMap(Texture2D voronoiTextureMap, Vector3[] vertices)
    {
        Texture2D texture = new Texture2D(width, height);

        float GetHeight(int x, int z, float defaultHeight)
        {

            if(x < 0 || x >= width || z < 0 || z >= height)
            {
                return defaultHeight;
            }

            var y = vertices[x + z * width].y;

            if(y == 0)
            {
                return defaultHeight / 2;
            }

            return vertices[x + z * width].y;
        }

        float GetSlope(int x, int z)
        {
            float height = GetHeight(x, z, 0);
            float heightLeft = GetHeight(x - 1, z, height);
            float heightRight = GetHeight(x + 1, z, height);
            float heightDown = GetHeight(x, z - 1, height);
            float heightUp = GetHeight(x, z + 1, height);

            float slopeX = heightLeft - heightRight;
            float slopeZ = heightDown - heightUp;

            var slope = new Vector2(slopeX, slopeZ);
            var slopeMagnitude = slope.magnitude;

            return slopeMagnitude;
        }

        List<Color32> colors = new List<Color32>();
        List<Color32> colors2 = new List<Color32>();
        Color32 sand = new Color32(255, 0, 0, 0);
        Color32 water = new Color32(0, 0, 0, 255);

        foreach (var vertex in vertices)
        {
            bool isLand = voronoiTextureMap.GetPixel((int)vertex.x * 4, (int)vertex.z * 4).r != 0;

            if (!isLand)
            {
                colors.Add(water);
                colors2.Add(water);
                continue;
            }

            float grayScaleValue = Mathf.InverseLerp(0.5f, 1.5f, vertex.y/4);
            byte grayScale = (byte)(grayScaleValue * 255);

            Color32 grayScaleFromNoise = new Color32(grayScale, grayScale, grayScale, 255);
            colors2.Add(grayScaleFromNoise);

            if (vertex.y < 1f)
            {
                colors.Add(sand);
                continue;
            }

            //Sand weighting is 0 at y = 1 and 1 at y = 0.5f anything below 0.5f is water
            float sandWeighting = vertex.y < 1.5f ? (1.5f - vertex.y) * 2 : 0;

            //Rocky terrain becomes more likely the higher we get.  After we can no longer have sand.
            float rockWeighting = vertex.y - 1.5f;

            //Grassy anywhere we are relatively flat
            float grassWeighting = Mathf.Clamp01(1 - GetSlope((int)vertex.x, (int)vertex.z));

            float waterWeighting = vertex.y < 0.5f ? 1000 : 0;

            Vector4 values = new(
                sandWeighting,
                rockWeighting,
                grassWeighting,
                waterWeighting
            );

            values.Normalize();

            colors.Add(new Color(values.x, values.y, values.z, 1));
        }

        //colors.Reverse();

        texture.SetPixels32(colors2.ToArray());

        texture.Apply();

        return texture;
    }

    private Mesh GenerateTerrainMesh(Texture2D voronoiTextureMap)
    {

        float[,] noiseMap = PerlinNoise.GenerateNoiseMap(width, height, scale, octaves, lacunarity, persistence, seed);

        //Calculate min and max noise values
        float minNoiseValue = float.MaxValue;
        float maxNoiseValue = float.MinValue;
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                bool isLand = voronoiTextureMap.GetPixel(x * 4, y * 4).r != 0;

                if (!isLand)
                {
                    continue;
                }

                if (noiseMap[x, y] < minNoiseValue)
                {
                    minNoiseValue = noiseMap[x, y];
                }

                if (noiseMap[x, y] > maxNoiseValue)
                {
                    maxNoiseValue = noiseMap[x, y];
                }
            }
        }


        InputGeometry inputGeometry = new InputGeometry();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                inputGeometry.AddPoint(y, x);
            }
        }

        TriangleNetMesh triangleNetMesh = new TriangleNetMesh();
        triangleNetMesh.Triangulate(inputGeometry);

        Vector3 CalculateHeight(TriangleNet.Data.Vertex vertex)
        {
            bool isLand = voronoiTextureMap.GetPixel((int)vertex.X * 4, (int)vertex.Y * 4).r != 0;

            if (!isLand)
            {
                return new Vector3((float)vertex.X, 0, (float)vertex.Y);
            }

            var noiseValue = noiseMap[(int)vertex.X, (int)vertex.Y];
            var normalizedNoiseValue = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseValue);

            //Default of 0.5f gurantees all land is at least 0.5f
            return new Vector3((float)vertex.X, normalizedNoiseValue * 4 + 0.5f, (float)vertex.Y);
        }

        Mesh mesh = new()
        {
            vertices = triangleNetMesh.Vertices.Select(v => CalculateHeight(v)).ToArray(),
            triangles = triangleNetMesh.Triangles.SelectMany(t => new int[] { t.P0, t.P1, t.P2 }).Reverse().ToArray(),
            uv = triangleNetMesh.Vertices.Select(v => new Vector2((float)v.X / width, (float)v.Y / height)).ToArray()
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    private void GenerateWorld()
    {
        //Higher this value is the larger the land masses will be.  Lower values will create more/smaller islands
        float territoryGrouping = 10;

        float[,] territoryTypeMap = PerlinNoise.GenerateNoiseMap(numberOfTerritories, numberOfTerritories, territoryGrouping, octaves, lacunarity, persistence, seed);

        VoronoiDiagram<Color32> voronoiDiagram = new VoronoiDiagram<Color32>(new Rect(0, 0, width * 4, height * 4));
        List<VoronoiDiagramSite<Color32>> sites = new List<VoronoiDiagramSite<Color32>>();

        int borderSpace = 4;
        float spacingX = Mathf.FloorToInt((width - borderSpace * 2.0f)/numberOfTerritories);
        float spacingY = Mathf.FloorToInt((height - borderSpace * 2.0f)/numberOfTerritories);

        using (new RandomState(seed))
        {
            for (int x = 1; x < numberOfTerritories; x++)
            {
                for (int y = 1; y < numberOfTerritories; y++)
                {
                    var xOffset = Random.Range(-spacingX / 3f, spacingX / 3f);
                    var yOffset = Random.Range(-spacingY / 3f, spacingY / 3f);

                    //when x = 0 the bounds are (255 - 8)/21 = 11.38  11.38/2 = 5.69  5.69 + 0 + random(-5.69, 5.69)

                    //Border space prevents us from getting too close to border.  
                    //SpacingX and SpacingY are the size of the territories
                    //xOffset and yOffset are random values to make the territories not perfectly square
                    var siteLocation = new Vector2(
                        borderSpace + (spacingX / 2) + (spacingX * x) + xOffset, 
                        borderSpace + (spacingY / 2) + (spacingY * y) + yOffset
                    );

                    bool isLand = territoryTypeMap[x, y] > 0.5f;

                    var randomColor = new Color32(isLand ? (byte)255 : (byte)0, (byte)x, (byte)y, 255);

                    sites.Add(new VoronoiDiagramSite<Color32>(siteLocation * 4, randomColor));
                }
            }
        }

        voronoiDiagram.AddSites(sites);
        voronoiDiagram.GenerateSites(1);

        Texture2D texture = new Texture2D(width * 4, height * 4);
        texture.SetPixels32(voronoiDiagram.Get1DSampleArray());
        texture.Apply();

        var mesh = GenerateTerrainMesh(texture);

        meshFilter.sharedMesh = mesh;


        var splatMap = GenerateSplatMap(texture, mesh.vertices);
        meshRenderer.material.mainTexture = splatMap;
    }
}
