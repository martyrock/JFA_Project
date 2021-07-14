using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voronoi_CPU : MonoBehaviour
{
    [SerializeField] private Material debugMat;
    [SerializeField] private int seeds = 10;
    [SerializeField] private int resolution = 1024;

    private Texture2D voronoiTexture;
    private int n;
    private JFA_DATA[,] textureData;
    private Queue<JFA_DATA> JFA_queue = new Queue<JFA_DATA>();
    public Color[] colors;

    class JFA_DATA
    {
        public bool isSeed;
        public Color color;
        public int[] seed;
        public int[] coords;
        public int k;
        public bool invalid;

        public JFA_DATA(bool isSeed, Color color, int[] coords, int[] seed, int k)
        {
            this.isSeed = isSeed;
            this.color = color;
            this.coords = coords;
            this.seed = seed;
            this.k = k;
            invalid = false;
        }
    }

    public void Start()
    {
        n = resolution;
        //colors = new Color[seeds];

        //for (int i = 0; i < colors.Length; i++)
        //{
        //    colors[i] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        //}

        textureData = new JFA_DATA[n, n];

        SetSeeds();
        StartCoroutine(CalculateVoronoi());
        //CalculateVoronoi();
        PopulateTexture();

        voronoiTexture.Apply();
        debugMat.mainTexture = voronoiTexture;
    }

    private void SetSeeds()
    {
        for (int i = 0; i < colors.Length; i++)
        {
            Color color = colors[i];
            int pixelX = UnityEngine.Random.Range(0, n);
            int pixelY = UnityEngine.Random.Range(0, n);
            textureData[pixelX, pixelY] = new JFA_DATA(true, color, new int[] { pixelX, pixelY }, new int[] { pixelX, pixelY }, n / 2);
            JFA_queue.Enqueue(textureData[pixelX, pixelY]);
        }
    }


    private IEnumerator CalculateVoronoi()
    {
        while (JFA_queue.Count > 0)
        {
            JFA_DATA pixel = JFA_queue.Dequeue();
            Color color = pixel.color;
            int k = pixel.k;
            int[] coords = pixel.coords;
            int[] seed = pixel.seed;

            if (k == 0 || pixel.invalid) continue;

            pixel.k = k / 2;
            JFA_queue.Enqueue(pixel);

            for (int x = -k; x <= k; x += k)
            {
                for (int y = -k; y <= k; y += k)
                {
                    int pixelX = coords[0] + x;
                    int pixelY = coords[1] + y;

                    if (!(x == 0 && y == 0) && pixelX >= 0 && pixelX < n && pixelY >= 0 && pixelY < n)
                    {
                        if (textureData[pixelX, pixelY] == null)
                        {
                            textureData[pixelX, pixelY] = new JFA_DATA(false, color, new int[] { pixelX, pixelY }, seed, k / 2);
                            JFA_queue.Enqueue(textureData[pixelX, pixelY]);
                        }
                        else
                        {
                            JFA_DATA old_pixel = textureData[pixelX, pixelY];

                            if (!old_pixel.isSeed && color != old_pixel.color)
                            {
                                int[] currentCoords = new int[] { pixelX, pixelY };
                                int[] otherSeed = old_pixel.seed;
                                float d1 = CalculateDistance(currentCoords, otherSeed);
                                float d2 = CalculateDistance(currentCoords, seed);

                                if (d2 < d1)
                                {
                                    textureData[pixelX, pixelY].color = color;
                                    textureData[pixelX, pixelY].seed = seed;
                                    //textureData[pixelX, pixelY].k = k/2;
                                    JFA_queue.Enqueue(textureData[pixelX, pixelY]);
                                }
                            }
                        }

                    }
                }
            }

            if (false)
            {
                PopulateTexture();
                voronoiTexture.Apply();
                debugMat.mainTexture = voronoiTexture;
                yield return null;
            }
        }
    }

    private float CalculateDistance(int[] p1, int[] p2)
    {
        Vector2 v1 = new Vector2(p1[0], p1[1]);
        Vector2 v2 = new Vector2(p2[0], p2[1]);
        return Vector2.Distance(v1, v2);
    }

    private void PopulateTexture()
    {
        voronoiTexture = new Texture2D(n, n, TextureFormat.RGB24, false);
        voronoiTexture.filterMode = FilterMode.Point;
        for (int width = 0; width < n; width++)
        {
            for (int height = 0; height < n; height++)
            {

                Color color = textureData[width, height]?.color ?? Color.black;
                color = (textureData[width, height] != null && textureData[width, height].isSeed) ? Color.white : color;
                voronoiTexture.SetPixel(width, height, color);
            }
        }
    }
}
