using System.Collections;
using Unity.Burst;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;

public class Voronoi_Jobs : MonoBehaviour
{
    [SerializeField] private Material debugMat;
    [SerializeField] private int seeds = 10;
    [SerializeField] private int resolution = 1024;

    [BurstCompile(CompileSynchronously =true)]
    struct VoronoiJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Seed> nativeSeeds;
        public int n;
        public int k;

        [NativeDisableParallelForRestriction]
        public NativeArray<PixelData> textureData;

        public void Execute(int index)
        {
            PixelData id = textureData[index];

            if (id.set)
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Coords at = new Coords();
                        at.x = (i - 1) * k + id.x;
                        at.y = (j - 1) * k + id.y;
                        if (at.x < 0 || at.y < 0 || at.x >= n || at.y >= n) continue;
                        if (textureData[at.x + at.y * n].set == false)
                        {
                            textureData[at.x + at.y * n] = new PixelData()
                            {
                                seedIndex = id.seedIndex,
                                isSeed = false,
                                set = true,
                                x = at.x,
                                y = at.y
                            };
                        }
                        else
                        {
                            Coords closest = nativeSeeds[id.seedIndex].coords;
                            Coords pixel = nativeSeeds[textureData[at.x + at.y * n].seedIndex].coords;
                            float idDist = CalculateDistance(at, closest);
                            float pixelDist = CalculateDistance(at, pixel);
                            if (idDist < pixelDist)
                            {
                                textureData[at.x + at.y * n] = new PixelData()
                                {
                                    seedIndex = id.seedIndex,
                                    isSeed = false,
                                    set = true,
                                    x = at.x,
                                    y = at.y
                                };
                            }
                        }
                    }
                }
            }
        }

        private float CalculateDistance(Coords p1, Coords p2)
        {
            Vector2 v1 = new Vector2(p1.x, p1.y);
            Vector2 v2 = new Vector2(p2.x, p2.y);
            return Vector2.Distance(v1, v2);
        }
    }

    struct Seed
    {
        public Color color;
        public Coords coords;
    }

    struct PixelData
    {
        public int seedIndex;
        public int x;
        public int y;
        public bool isSeed;
        public bool set;
    }

    struct Coords
    {
        public int x;
        public int y;

        public Coords(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    private Texture2D voronoiTexture;
    private int n;
    private NativeArray<Seed> nativeSeeds;
    private NativeArray<PixelData> textureData;

    private IEnumerator Start()
    {
        n = resolution;
        nativeSeeds = new NativeArray<Seed>(seeds, Allocator.Persistent);
        textureData = new NativeArray<PixelData>(n * n, Allocator.Persistent);

        InitializaTextureData();
        InitializeWithColors();

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        float timer = Time.realtimeSinceStartup;
        JobHandle jobHandle = default;
        for (int k = n / 2; k >= 1; k /= 2)
        {
            VoronoiJob voronoiJob = new VoronoiJob()
            {
                nativeSeeds = nativeSeeds,
                n = n,
                k = k,
                textureData = textureData,
            };
            jobHandle = voronoiJob.Schedule(n * n, n / 8, jobHandle);

        }
        jobHandle.Complete();
        timer = Time.realtimeSinceStartup - timer;
        Debug.Log("Time: " + timer);

        PopulateTexture();

        nativeSeeds.Dispose();
        textureData.Dispose();
    }

    private void InitializeWithColors()
    {
        for (int i = 0; i < seeds; i++)
        {
            Seed seed = new Seed()
            {
                color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value),
                coords = new Coords(UnityEngine.Random.Range(0, n), UnityEngine.Random.Range(0, n)),
            };
            nativeSeeds[i] = seed;

            PixelData pixel = textureData[seed.coords.x + seed.coords.y * n];
            pixel.seedIndex = i;
            pixel.isSeed = false;
            pixel.set = true;
            textureData[seed.coords.x + seed.coords.y * n] = pixel;
        }
    }

    private void InitializaTextureData()
    {
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                textureData[i * n + j] = new PixelData()
                {
                    seedIndex = 0,
                    x = j,
                    y = i,
                    isSeed = false,
                    set = false
                };
            }
        }
    }

    private void PopulateTexture()
    {
        voronoiTexture = new Texture2D(n, n, TextureFormat.RGB24, false);
        voronoiTexture.filterMode = FilterMode.Point;
        for (int width = 0; width < n; width++)
        {
            for (int height = 0; height < n; height++)
            {
                Color color = nativeSeeds[textureData[width + height * n].seedIndex].color;
                voronoiTexture.SetPixel(width, height, color);
            }
        }
        voronoiTexture.Apply();
        debugMat = GetComponent<MeshRenderer>().material;
        debugMat.mainTexture = voronoiTexture;
    }
}
