using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voronoi_GPU : MonoBehaviour
{
    [SerializeField] ComputeShader voronoiCompute;
    [SerializeField] Material mat;
    [SerializeField] int resolution;
    [SerializeField] int seeds = 10;

    private int preprocessKernelHandle;
    private int pre_groupSizeX;
    private int jfaKernelHandle;
    private int jfa_groupSize;
    private int postKernelHandle;
    private int post_groupSize;

    private ComputeBuffer color_seedsCompute;

    private RenderTexture voronoiTex;
    private int n;
    [SerializeField] private Color[] colors;

#region JFA_DATA
    struct JFA_DATA
    {
        public Color color;
        public Vector2 coords;

        public JFA_DATA(Color color, Vector2 coords)
        {
            this.color = color;
            this.coords = coords;
        }
    }
#endregion

    private void Start()
    {
        n = resolution;

        InitializeColors();
        InitializeTexture();

        InitializeComputeShader();
        InitializeComputeBuffers();
        StartCoroutine(ComputeVoronoi());
        //ComputeVoronoi();
    }

    private void InitializeColors()
    {
        colors = new Color[seeds];
        for (int i = 0; i < seeds; i++)
        {
            colors[i] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }
    }

    private void InitializeTexture()
    {
        voronoiTex = RenderTexture.GetTemporary(n, n, 0,RenderTextureFormat.ARGBHalf);
        voronoiTex.enableRandomWrite = true;
        mat.mainTexture = voronoiTex;
    }

    private IEnumerator ComputeVoronoi()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        voronoiCompute.SetInt("n", n);
        voronoiCompute.SetTexture(preprocessKernelHandle, "Voronoi", voronoiTex);
        voronoiCompute.SetBuffer(preprocessKernelHandle, "seeds", color_seedsCompute);
        voronoiCompute.Dispatch(preprocessKernelHandle, pre_groupSizeX, 1, 1);

        voronoiCompute.SetTexture(jfaKernelHandle, "Voronoi", voronoiTex);
        voronoiCompute.SetBuffer(jfaKernelHandle, "seeds", color_seedsCompute);

        float timer = Time.realtimeSinceStartup;
        for (int k = n / 2; k >= 1; k /= 2)
        {
            voronoiCompute.SetInt("k", k);
            voronoiCompute.Dispatch(jfaKernelHandle, jfa_groupSize, jfa_groupSize, 1);
        }
        JFA_DATA[] result = new JFA_DATA[seeds];
        color_seedsCompute.GetData(result); //Force this thread to wait for the compute shader dispatch
        timer = Time.realtimeSinceStartup - timer;
        Debug.Log("Time: " + timer);

        voronoiCompute.SetTexture(postKernelHandle, "Voronoi", voronoiTex);
        voronoiCompute.SetBuffer(postKernelHandle, "seeds", color_seedsCompute);
        voronoiCompute.Dispatch(postKernelHandle, post_groupSize, post_groupSize, 1);
    }

    private void InitializeComputeBuffers()
    {
        JFA_DATA[] jfa_data = new JFA_DATA[seeds];
        for (int i = 0; i < seeds; i++)
        {
            jfa_data[i] = new JFA_DATA(colors[i], 
                            new Vector2(UnityEngine.Random.Range(0, n), 
                                UnityEngine.Random.Range(0, n)));
        }

        color_seedsCompute = new ComputeBuffer(seeds, sizeof(float) * 6);
        color_seedsCompute.SetData(jfa_data);
    }

    private void InitializeComputeShader()
    {
        uint x;
        preprocessKernelHandle = voronoiCompute.FindKernel("PreProcess");
        voronoiCompute.GetKernelThreadGroupSizes(preprocessKernelHandle, out x, out _, out _);
        pre_groupSizeX = Mathf.CeilToInt(seeds/ (float)x);

        jfaKernelHandle = voronoiCompute.FindKernel("JFA");
        voronoiCompute.GetKernelThreadGroupSizes(jfaKernelHandle, out x, out _, out _);
        jfa_groupSize = Mathf.CeilToInt(n / (float)x);

        postKernelHandle = voronoiCompute.FindKernel("PostProcess");
        voronoiCompute.GetKernelThreadGroupSizes(postKernelHandle, out x, out _, out _);
        post_groupSize = Mathf.CeilToInt(n / (float)x);
    }

    void OnDisable()
    {
        if (color_seedsCompute != null)
        {
            color_seedsCompute.Release();
            color_seedsCompute = null;
        }
    }
}
