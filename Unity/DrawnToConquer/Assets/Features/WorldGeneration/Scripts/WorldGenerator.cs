using MoreLinq;
using PixelsForGlory.VoronoiDiagram;
using System.Collections.Generic;
using System.Linq;
using TriangleNet;
using TriangleNet.Geometry;
using Unity.AI.Navigation;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private GameObject planeDebug;
    [SerializeField] private GameObject planeDebug2;
    [SerializeField] private GameObject planeDebug3;

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

    //private Texture2D GenerateSplatMap(Texture2D voronoiTextureMap, Vector3[] vertices)
    //{
    //    Texture2D texture = new Texture2D(width, height);

    //    float GetHeight(int x, int z, float defaultHeight)
    //    {

    //        if(x < 0 || x >= width || z < 0 || z >= height)
    //        {
    //            return defaultHeight;
    //        }

    //        var y = vertices[x + z * width].y;

    //        if(y == 0)
    //        {
    //            return defaultHeight / 2;
    //        }

    //        return vertices[x + z * width].y;
    //    }

    //    float GetSlope(int x, int z)
    //    {
    //        float height = GetHeight(x, z, 0);
    //        float heightLeft = GetHeight(x - 1, z, height);
    //        float heightRight = GetHeight(x + 1, z, height);
    //        float heightDown = GetHeight(x, z - 1, height);
    //        float heightUp = GetHeight(x, z + 1, height);

    //        float slopeX = heightLeft - heightRight;
    //        float slopeZ = heightDown - heightUp;

    //        var slope = new Vector2(slopeX, slopeZ);
    //        var slopeMagnitude = slope.magnitude;

    //        return slopeMagnitude;
    //    }

    //    List<Color32> colors = new List<Color32>();
    //    List<Color32> colors2 = new List<Color32>();
    //    Color32 sand = new Color32(255, 0, 0, 0);
    //    Color32 water = new Color32(0, 0, 0, 255);

    //    foreach (var vertex in vertices)
    //    {
    //        bool isLand = voronoiTextureMap.GetPixel((int)vertex.x * 4, (int)vertex.z * 4).r != 0;

    //        if (!isLand)
    //        {
    //            colors.Add(water);
    //            colors2.Add(water);
    //            continue;
    //        }

    //        float grayScaleValue = Mathf.InverseLerp(0.5f, 1.5f, vertex.y/4);
    //        byte grayScale = (byte)(grayScaleValue * 255);

    //        Color32 grayScaleFromNoise = new Color32(grayScale, grayScale, grayScale, 255);
    //        colors2.Add(grayScaleFromNoise);

    //        if (vertex.y < 1f)
    //        {
    //            colors.Add(sand);
    //            continue;
    //        }

    //        //Sand weighting is 0 at y = 1 and 1 at y = 0.5f anything below 0.5f is water
    //        float sandWeighting = vertex.y < 1.5f ? (1.5f - vertex.y) * 2 : 0;

    //        //Rocky terrain becomes more likely the higher we get.  After we can no longer have sand.
    //        float rockWeighting = vertex.y - 1.5f;

    //        //Grassy anywhere we are relatively flat
    //        float grassWeighting = Mathf.Clamp01(1 - GetSlope((int)vertex.x, (int)vertex.z));

    //        float waterWeighting = vertex.y < 0.5f ? 1000 : 0;

    //        Vector4 values = new(
    //            sandWeighting,
    //            rockWeighting,
    //            grassWeighting,
    //            waterWeighting
    //        );

    //        values.Normalize();

    //        colors.Add(new Color(values.x, values.y, values.z, 1));
    //    }

    //    //colors.Reverse();

    //    texture.SetPixels32(colors2.ToArray());

    //    texture.Apply();

    //    return texture;
    //}

    private Texture2D GenerateSplatMap(LookupBuffer<Color32> combinedBiomeMap)
    {
        Texture2D texture = new Texture2D(width * 4, height * 4);

        List<Color32> colors = new List<Color32>();

        Dictionary<byte, Color32> biomeColors = new Dictionary<byte, Color32>
        {
            { 0, new Color32(0, 0, 0, 0) },
            { 32, new Color32(255, 0, 0, 0) },
            { 64, new Color32(0, 255, 0, 0) },
            { 128, new Color32(0, 0, 255, 0) },
            { 255, new Color32(0, 0, 0, 255) }
        };

        for (int x = 0; x < width * 4; x++)
        {
            for (int y = 0; y < height * 4; y++)
            {
                var biome = combinedBiomeMap.Get(y, x).r;
                var color = biomeColors[biome];
                colors.Add(color);
            }
        }

        //colors.Reverse();

        texture.SetPixels32(colors.ToArray());

        texture.Apply();

        return texture;
    }

    private (List<Vector3> vertices, List<int> triangles, List<Vector2> uvs) GenerateTerrainMesh(LookupBuffer<Color32> voronoiTextureMap, LookupBuffer<Color32> voronoiBiomeMap, List<Thresholds> biomes, List<Vector3> voronoiEdgeVertices)
    {

        float[,] noiseMap = PerlinNoise.GenerateNoiseMap(width, height, scale, octaves, lacunarity, persistence, seed);

        //Calculate min and max noise values
        float minNoiseValue = float.MaxValue;
        float maxNoiseValue = float.MinValue;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isLand = voronoiTextureMap.Get(x * 4, y * 4).r != 0;

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

        List<Vector2Int> GetNeighbors(int x, int y)
        {
            return new List<Vector2Int>
            {
                new Vector2Int(x - 1, y),
                new Vector2Int(x + 1, y),
                new Vector2Int(x, y - 1),
                new Vector2Int(x, y + 1),
                new Vector2Int(x - 1, y - 1),
                new Vector2Int(x + 1, y + 1),
                new Vector2Int(x - 1, y + 1),
                new Vector2Int(x + 1, y - 1)
            };
        }

        int smallStepSize = 4;

        InputGeometry inputGeometry = new();
        //for (int x = 0; x < width; x++)
        //{
        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int smallStepX = 0; smallStepX < smallStepSize; smallStepX+=2)
        //        {
        //            for (int smallStepY = 0; smallStepY < smallStepSize; smallStepY+=2)
        //            {

        //                if(
        //                    //Make sure to add all the points on the edge of the map
        //                    x != 0 
        //                    && y != 0 
        //                    && x != width - 1 
        //                    && y != height - 1

        //                    //Checking specifically water points
        //                    && voronoiTextureMap.Get(y * 4 + smallStepY, x * 4 + smallStepX).r == 0

        //                    //Checking if all neighbors are water
        //                    && GetNeighbors(y * 4 + smallStepY, x * 4 + smallStepX).All(neighbor => voronoiTextureMap.Get(neighbor.x, neighbor.y).r == 0)

        //                    //Checking if all neighbors of neighbors are water
        //                    && GetNeighbors(y * 4 + smallStepY, x * 4 + smallStepX).All(neighbor => GetNeighbors(neighbor.x, neighbor.y).All(n2 => voronoiTextureMap.Get(n2.x, n2.y).r == 0))
        //                ) {
        //                    continue;
        //                }

        //                inputGeometry.AddPoint(y + (smallStepY / 4), x + (smallStepX / 4));
        //            }
        //        }
        //    }
        //}

        //Add vertices around the border
        for(int x = 0; x < width; x++)
        {
            inputGeometry.AddPoint(0, x);
            inputGeometry.AddPoint(height-1, x);
        }

        for (int y = 0; y < height; y++)
        {
            inputGeometry.AddPoint(y, 0);
            inputGeometry.AddPoint(y, width - 1);
        }

        foreach (var vertice in voronoiEdgeVertices)
        {
            inputGeometry.AddPoint(vertice.x, vertice.z);
        }

        TriangleNetMesh triangleNetMesh = new TriangleNetMesh();
        triangleNetMesh.Triangulate(inputGeometry);

        Vector3 CalculateHeight(TriangleNet.Data.Vertex vertex)
        {

            int roundedX = Mathf.RoundToInt((float)vertex.X * 4);
            int roundedY = Mathf.RoundToInt((float)vertex.Y * 4);

            bool isLand = 0 != voronoiTextureMap.Get(
                roundedX,
                roundedY
            ).r;

            if (!isLand)
            {
                return new Vector3((float)vertex.X, 0, (float)vertex.Y);
            }

            //Very inefficient.  Replace later.
            Thresholds biome = biomes.FirstOrDefault(biome => Mathf.RoundToInt(voronoiBiomeMap.Get(roundedX, roundedY).r) == biome.redIdentifier);

            //var noiseValue = noiseMap[(int)vertex.X, (int)vertex.Y];
            //var normalizedNoiseValue = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseValue);

            //Default of 0.5f gurantees all land is at least 0.5f
            return new Vector3((float)vertex.X, biome.targetHeight, (float)vertex.Y);
        }

        //Mesh mesh = new()
        //{
        //    vertices = triangleNetMesh.Vertices.Select(v => CalculateHeight(v)).ToArray(),
        //    triangles = triangleNetMesh.Triangles.SelectMany(t => new int[] { t.P0, t.P1, t.P2 }).Reverse().ToArray(),
        //    uv = triangleNetMesh.Vertices.Select(v => new Vector2((float)v.X / width, (float)v.Y / height)).ToArray()
        //};

        //mesh.RecalculateBounds();
        //mesh.RecalculateNormals();
        //mesh.RecalculateTangents();

        //return mesh;

        return (
            triangleNetMesh.Vertices.Select(v => CalculateHeight(v)).ToList(),
            triangleNetMesh.Triangles.SelectMany(t => new int[] { t.P0, t.P1, t.P2 }).Reverse().ToList(),
            triangleNetMesh.Vertices.Select(v => new Vector2((float)v.X / width, (float)v.Y / height)).ToList()
        );
    }

    struct Thresholds
    {
        public float max;
        public string name;
        public byte redIdentifier;

        //Eventually change this to a customer generator
        public float targetHeight;
    }

    private VoronoiDiagram<Color32> GenerateVoronoi(int numberOfCells, float[,] noiseMap, List<Thresholds> thresholds)
    {

        //TODO: Add ability to group voronois together to create larger territories with greater resolution.
        //All grouped voronois in a group will need to share the same "red identifier" from the first one in the group

        VoronoiDiagram<Color32> voronoiDiagram = new VoronoiDiagram<Color32>(new Rect(0, 0, width * 4, height * 4));
        List<VoronoiDiagramSite<Color32>> sites = new List<VoronoiDiagramSite<Color32>>();

        //Border space is the space between the edge of the map and the first territory. x2 because it is on both sides
        int borderSpace = 4;
        float spacingX = Mathf.FloorToInt((width - borderSpace * 2.0f) / numberOfCells);
        float spacingY = Mathf.FloorToInt((height - borderSpace * 2.0f) / numberOfCells);

        using (new RandomState(seed))
        {
            for (int x = 1; x < numberOfCells; x++)
            {
                for (int y = 1; y < numberOfCells; y++)
                {
                    //Find an offset that is a fraction of the spacing.  Ensure it is smaller than spacing/2
                    var xOffset = Random.Range(-spacingX / 3f, spacingX / 3f);
                    var yOffset = Random.Range(-spacingY / 3f, spacingY / 3f);

                    //Border space prevents us from getting too close to border.  
                    //SpacingX and SpacingY are the size of the territories
                    //xOffset and yOffset are random values to make the territories not perfectly square
                    var siteLocation = new Vector2(
                        borderSpace + (spacingX / 2) + (spacingX * x) + xOffset,
                        borderSpace + (spacingY / 2) + (spacingY * y) + yOffset
                    );

                    //Red identier will be 0 if no threshold is found
                    var threshold = thresholds.FirstOrDefault(thresholds => noiseMap[x, y] < thresholds.max);

                    var randomColor = new Color32(threshold.redIdentifier, (byte)x, (byte)y, 255);

                    sites.Add(new VoronoiDiagramSite<Color32>(siteLocation * 4, randomColor));
                }
            }
        }

        voronoiDiagram.AddSites(sites);
        voronoiDiagram.GenerateSites(1);

        return voronoiDiagram;
    }

    private class LookupBuffer<T>{
        int width;
        int height;
        T[] buffer;

        public LookupBuffer(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.buffer = new T[width*height];
        }

        public LookupBuffer(int width, int height, T[] buffer)
        {
            this.width = width;
            this.height = height;
            this.buffer = buffer;
        }

        public void Set(int x, int y, T value)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            buffer[x + y * width] = value;
        }

        public T Get(int x, int y, T defaultReturn = default)
        {
            if(x < 0 || x >= width || y < 0 || y >= height)
            {
                return defaultReturn;
            }

            return buffer[x + y * width];
        }

        public T[] values => buffer;
    }

    private void GenerateWorld()
    {
        //Higher this value is the larger the land masses will be.  Lower values will create more/smaller islands
        float territoryGrouping = 10;
        float biomeGrouping = 5;

        float[,] territoryTypeMap = PerlinNoise.GenerateNoiseMap(numberOfTerritories, numberOfTerritories, territoryGrouping, octaves, lacunarity, persistence, seed);
        float[,] biomeMap = PerlinNoise.GenerateNoiseMap(numberOfTerritories*4, numberOfTerritories*4, biomeGrouping, octaves, lacunarity, persistence, seed);
        float[,] terrainTextureMap = PerlinNoise.GenerateNoiseMap(width * 4, height * 4, scale, octaves, lacunarity, persistence, seed);

        var territoryVoronoi = GenerateVoronoi(numberOfTerritories, territoryTypeMap, new List<Thresholds>
        {
            new Thresholds { max = 0.5f, name = "Water", redIdentifier = 0 },
            new Thresholds { max = 1f, name = "Land", redIdentifier = 255 }
        });

        var biomeThresholds = new List<Thresholds>
        {
            new Thresholds{ max = 0f, name = "Beach", redIdentifier = 32, targetHeight = 0.5f},
            new Thresholds { max = 0.3f, name = "Mountain", redIdentifier = 255, targetHeight = 2},
            new Thresholds { max = 0.4f, name = "Forest", redIdentifier = 128, targetHeight = 1 },
            new Thresholds { max = 0.7f, name = "Grassland", redIdentifier = 64, targetHeight = 1 },
            new Thresholds { max = 0.85f, name = "Forest", redIdentifier = 128, targetHeight = 1 },
            new Thresholds { max = 1f, name = "Grassland", redIdentifier = 64, targetHeight = 1 }
        };

        var biomeVoronoi = GenerateVoronoi(numberOfTerritories * 4, biomeMap, biomeThresholds);

        LookupBuffer<Color32> landTypeLookup = new LookupBuffer<Color32>(width * 4, height * 4, territoryVoronoi.Get1DSampleArray());
        LookupBuffer<Color32> biomeLookup = new LookupBuffer<Color32>(width * 4, height * 4, biomeVoronoi.Get1DSampleArray());
        LookupBuffer<Color32> terrainTexture = new LookupBuffer<Color32>(width * 4, height * 4);

        byte SampleLandType(int x2, int y2)
        {
            if ((x2 < 0) || (x2 >= (width * 4)) || (y2 < 0) || (y2 >= (height * 4)))
            {
                var a = (x2 < 0) || (x2 >= (width * 4)) || (y2 < 0) || (y2 >= (height * 4));
                var b = x2 < 0;
                var c = x2 >= (width * 4);
                var d = y2 < 0;
                var e = y2 >= (height * 4);
                var f = b || c || d || e;
                return 255;
            }

            return landTypeLookup.Get(x2, y2).r;
        }

        List<Vector2Int> GetNeighbors(int x, int y)
        {
            return new List<Vector2Int>
            {
                new Vector2Int(x - 1, y),
                new Vector2Int(x + 1, y),
                new Vector2Int(x, y - 1),
                new Vector2Int(x, y + 1),
                new Vector2Int(x - 1, y - 1),
                new Vector2Int(x + 1, y + 1),
                new Vector2Int(x - 1, y + 1),
                new Vector2Int(x + 1, y - 1)
            };
        } 

        Color32 beach = new Color32(0, 255, 0, 255);
        foreach (var x in Enumerable.Range(0, width * 4))
        {
            foreach(var y in Enumerable.Range(0, height * 4))
            {
                var landType = landTypeLookup.Get(x, y);

                if(landType.r == 0)
                {
                    continue;
                }

                //Neighbors
                List<Vector2Int> neighbors = GetNeighbors(x, y);

                // Check if we are on land but neighbor water
                if (neighbors.Any(neighbor => SampleLandType(neighbor.x, neighbor.y) == 0) && biomeLookup.Get(x, y).r != 255)
                {
                    biomeLookup.Set(x, y, beach);
                    //Set surrounding pixels to beach
                    foreach (var neighbor in neighbors.SelectMany(neighbor => GetNeighbors(neighbor.x, neighbor.y)))
                    {
                        if (SampleLandType(neighbor.x, neighbor.y) == 255)
                        {
                            //Don't overwrite mountain
                            if (biomeLookup.Get(neighbor.x, neighbor.y).r != 255)
                            {
                                biomeLookup.Set(neighbor.x, neighbor.y, new Color32(32, 255, 255, 255));
                            }
                        }
                    }
                }

                var terrainType = biomeLookup.Get(x, y);
                var terrainNoise = terrainTextureMap[x, y];

                if (terrainType.r == 0)
                {
                    terrainTexture.Set(x, y, new Color32(0, 0, 0, 255));
                }else if(terrainType.r == 32)
                {
                    Color32 sand = Color32.Lerp(new Color32(215, 192, 158, 0), new Color32(255, 246, 193, 0), terrainNoise);
                    terrainTexture.Set(x, y, sand);
                }else if (terrainType.r == 64)
                {
                    Color32 grass = Color32.Lerp(new Color32(2, 166, 155, 0), new Color32(60, 239, 124, 0), terrainNoise);
                    terrainTexture.Set(x, y, grass);
                }else if (terrainType.r == 128)
                {
                    Color32 forest = Color32.Lerp(new Color32(22, 181, 141, 0), new Color32(10, 145, 113, 0), terrainNoise);
                    terrainTexture.Set(x, y, forest);
                }else if (terrainType.r == 255)
                {
                    Color32 mountain = Color32.Lerp(new Color32(73, 60, 60, 0), new Color32(158, 157, 156, 0), terrainNoise);
                    terrainTexture.Set(x, y, mountain);
                }
            }
        }

        IEnumerable<Vector3> calculateAllLerped(Vector3 a, Vector3 b, float sizeBetweenSteps)
        {

            var distance = Vector3.Distance(a, b);
            var steps = Mathf.FloorToInt(distance / 4f);

            if (steps == 0)
            {
                yield return a / 4;
                yield return b / 4;
                yield break;
            }

            for (int i = 0; i <= steps; i++)
            {
                yield return Vector3.Lerp(a, b, (float)i / steps) / 4;
            }
        }

        HashSet<(Vector3Int, Vector3Int)> seenVertices = new();

        List<Vector3> voronoiTerritoryEdgeVertices = new();
        foreach(var site in territoryVoronoi.GeneratedSites)
        {
            foreach(var edge in site.Value.Edges)
            {
                var leftPoint = edge.LeftEndPoint.ToVector3();
                var rightPoint = edge.RightEndPoint.ToVector3();

                if (seenVertices.Contains((leftPoint.ToVector3Int(), rightPoint.ToVector3Int())) || seenVertices.Contains((rightPoint.ToVector3Int(), leftPoint.ToVector3Int())))
                {
                    continue;
                }

                seenVertices.Add((leftPoint.ToVector3Int(), rightPoint.ToVector3Int()));

                //Calculate a line parallel to the edge that is 0.1 units away from the edge
                var edgeDirection = (rightPoint - leftPoint).normalized;
                var edgeNormal = new Vector3(-edgeDirection.z, 0, edgeDirection.x);

                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint, rightPoint, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint + edgeNormal, rightPoint + edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint + edgeNormal * 2, rightPoint + edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint + edgeNormal * 5, rightPoint + edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint - edgeNormal, rightPoint - edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint - edgeNormal * 2, rightPoint - edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint - edgeNormal * 5, rightPoint - edgeNormal, 1f));
            }
        }

        foreach(var site in biomeVoronoi.GeneratedSites)
        {
            foreach (var edge in site.Value.Edges)
            {
                var leftPoint = edge.LeftEndPoint.ToVector3();
                var rightPoint = edge.RightEndPoint.ToVector3();

                if (seenVertices.Contains((leftPoint.ToVector3Int(), rightPoint.ToVector3Int())) || seenVertices.Contains((rightPoint.ToVector3Int(), leftPoint.ToVector3Int())))
                {
                    continue;
                }

                seenVertices.Add((leftPoint.ToVector3Int(), rightPoint.ToVector3Int()));

                //Calculate a line parallel to the edge that is 0.1 units away from the edge
                var edgeDirection = (rightPoint - leftPoint).normalized;
                var edgeNormal = new Vector3(-edgeDirection.z, 0, edgeDirection.x);

                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint, rightPoint, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint + edgeNormal, rightPoint + edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint + edgeNormal * 2, rightPoint + edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint + edgeNormal * 5, rightPoint + edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint - edgeNormal, rightPoint - edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint - edgeNormal * 2, rightPoint - edgeNormal, 1f));
                voronoiTerritoryEdgeVertices.AddRange(calculateAllLerped(leftPoint - edgeNormal * 5, rightPoint - edgeNormal, 1f));
            }
        }

        Color32 blue = new Color32(0, 0, 255, 255);

        var combinedLookup = new LookupBuffer<Color32>(width * 4, height * 4, landTypeLookup.values.Zip(biomeLookup.values, (a, b) => a.r != 255 ? blue : b).ToArray());

        Texture2D landTypeTexture2D = new(width * 4, height * 4);
        landTypeTexture2D.SetPixels32(landTypeLookup.values);
        landTypeTexture2D.Apply();

        Texture2D combinedTextures = new(width * 4, height * 4);
        combinedTextures.SetPixels32(combinedLookup.values);
        combinedTextures.Apply();

        var terrainTexture2D = new Texture2D(width * 4, height * 4);
        terrainTexture2D.SetPixels32(terrainTexture.values);
        terrainTexture2D.Apply();

        planeDebug.GetComponent<MeshRenderer>().material.mainTexture = combinedTextures;
        planeDebug2.GetComponent<MeshRenderer>().material.mainTexture = terrainTexture2D;
        planeDebug3.GetComponent<MeshRenderer>().material.mainTexture = landTypeTexture2D;

        var (vertices, triangles, uvs) = GenerateTerrainMesh(landTypeLookup, biomeLookup, biomeThresholds, voronoiTerritoryEdgeVertices);

        Debug.Log("Starting to generate chunks");

        List<Mesh> chunks = GenerateChunks(BuildAdjacency(vertices, triangles), triangles, uvs, 50000);

        Debug.Log("Done generating chunks");

        //var splatTexture = GenerateSplatMap(combinedLookup);

        foreach (var chunk in chunks)
        {
            GameObject chunkObject = new GameObject("Chunk");
            chunkObject.transform.parent = transform;
            chunkObject.transform.localPosition = Vector3.zero;
            chunkObject.transform.localRotation = Quaternion.identity;
            chunkObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = chunk;

            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

            meshRenderer.material = baseMaterial;
            meshRenderer.material.mainTexture = terrainTexture2D;
            //meshRenderer.material.mainTexture = landTypeTexture2D;

            var collider = chunkObject.AddComponent<MeshCollider>();
            collider.convex = true;
        }

        var surface = gameObject.GetComponent<NavMeshSurface>();
        surface.agentTypeID = NavMesh.GetSettingsByIndex(1).agentTypeID;
        surface.BuildNavMesh();

        //gameObject.SetActive(false);

        //meshFilter.sharedMesh = mesh;

        //meshRenderer.material.mainTexture = GenerateSplatMap(combinedLookup);
    }

    //Break large mesh up into smaller meshes
    private List<Mesh> GenerateChunks(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, int maxVerticesPerMesh)
    {

        List<Mesh> meshes = new();

        var triangleBatch = triangles.Batch(3).Batch(maxVerticesPerMesh/3);

        foreach (var batch in triangleBatch)
        {
            Dictionary<int, int> seenVertices = new();
            int vertexCount = 0;
            Mesh chunk = new();

            List<Vector3> newVertices = new(maxVerticesPerMesh);
            List<Vector2> newUvs = new(maxVerticesPerMesh);
            List<int> newTriangles = new(maxVerticesPerMesh/3);

            foreach (var triangle in batch)
            {
                foreach(var vertex in triangle)
                {
                    if (!seenVertices.ContainsKey(vertex))
                    {
                        seenVertices.Add(vertex, vertexCount++);
                        newVertices.Add(vertices[vertex]);
                        newUvs.Add(uvs[vertex]);
                    }
                    newTriangles.Add(seenVertices[vertex]);
                }

            }

            chunk.vertices = newVertices.ToArray();
            chunk.uv = newUvs.ToArray();
            chunk.triangles = newTriangles.ToArray();

            chunk.RecalculateBounds();
            chunk.RecalculateNormals();
            chunk.RecalculateTangents();

            meshes.Add(chunk);
        }

        return meshes;
    }

    /// <summary>
    /// Build a neighbor list for each vertex by examining the mesh triangles.
    /// </summary>
    private List<Vector3> BuildAdjacency(List<Vector3> vertices, List<int> _triangles)
    {
        int vertexCount = vertices.Count;
        List<int>[] adjacencyList = new List<int>[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            adjacencyList[i] = new List<int>();
        }

        // Examine each triangle in the mesh
        int[] triangles = _triangles.ToArray();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            // Add neighbors for each corner of this triangle
            if (!adjacencyList[i1].Contains(i2)) adjacencyList[i1].Add(i2);
            if (!adjacencyList[i1].Contains(i3)) adjacencyList[i1].Add(i3);

            if (!adjacencyList[i2].Contains(i1)) adjacencyList[i2].Add(i1);
            if (!adjacencyList[i2].Contains(i3)) adjacencyList[i2].Add(i3);

            if (!adjacencyList[i3].Contains(i1)) adjacencyList[i3].Add(i1);
            if (!adjacencyList[i3].Contains(i2)) adjacencyList[i3].Add(i2);
        }

        return SmoothMeshHeights(vertices, adjacencyList, 5, 0.5f);
    }

    /// <summary>
    /// Smooth the Y heights of the mesh vertices using a simple neighbor-average approach.
    /// </summary>
    private List<Vector3> SmoothMeshHeights(List<Vector3> vertices, List<int>[] adjacencyList, int smoothingIterations, float blendFactor) 
    {

        // We only want to adjust the y-component (height) while preserving x and z
        for (int iteration = 0; iteration < smoothingIterations; iteration++)
        {
            Vector3[] newVertices = new Vector3[vertices.Count];

            for (int v = 0; v < vertices.Count; v++)
            {
                // Current vertex
                Vector3 currentVertex = vertices[v];

                if(currentVertex.y == 0)
                {
                    newVertices[v] = currentVertex;
                    continue;
                }

                // Calculate the average height of neighbors
                float sumHeights = currentVertex.y;
                int neighborCount = 1;  // start with 1 to include the vertex itself

                foreach (int neighborIndex in adjacencyList[v])
                {
                    sumHeights += vertices[neighborIndex].y;
                    neighborCount++;
                }

                float averageHeight = sumHeights / neighborCount;

                // Blend our current height toward the average height
                float blendedHeight = Mathf.Lerp(currentVertex.y, averageHeight, blendFactor);

                // Assign the new vertex position
                newVertices[v] = new Vector3(currentVertex.x, blendedHeight, currentVertex.z);
            }

            // Update the working vertices array for the next iteration
            vertices = newVertices.ToList();
        }

        return vertices;
    }
}
