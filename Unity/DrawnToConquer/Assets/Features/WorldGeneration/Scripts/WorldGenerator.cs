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
    }

    private Mesh GenerateTerrainMesh(Texture2D voronoiTextureMap)
    {

        float[,] noiseMap = PerlinNoise.GenerateNoiseMap(width, height, scale, octaves, lacunarity, persistence, seed);

        InputGeometry inputGeometry = new InputGeometry();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                inputGeometry.AddPoint(x, y);
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

            //Default of 0.5f gurantees all land is at least 0.5f
            return new Vector3((float)vertex.X, noiseMap[(int)vertex.X, (int)vertex.Y] * 4 + 0.5f, (float)vertex.Y);
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

        meshRenderer.material.mainTexture = texture;
    }
}
