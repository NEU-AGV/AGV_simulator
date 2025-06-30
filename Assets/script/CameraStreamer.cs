using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

public class CameraStreamer : MonoBehaviour
{
    public float sendInterval = 0.03f; // 30ms ≈ 33fps
    public string serverUrl = "http://localhost:8000/stream";

    private Camera captureCamera;
    private RenderTexture renderTexture;
    private Texture2D tempTex;
    private HttpClient httpClient;

    private ConcurrentQueue<FrameData> frameQueue = new ConcurrentQueue<FrameData>();

    void Start()
    {
        Application.runInBackground = true;

        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("❌ 主相机不存在，请确认相机打上 MainCamera 标签！");
            return;
        }

        renderTexture = new RenderTexture(1440, 1080, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        GameObject camObj = new GameObject("CaptureCamera");
        captureCamera = camObj.AddComponent<Camera>();
        captureCamera.CopyFrom(mainCam);
        captureCamera.enabled = false;
        captureCamera.targetTexture = renderTexture;

        tempTex = new Texture2D(1440, 1080, TextureFormat.RGB24, false);
        httpClient = new HttpClient();

        StartCoroutine(CaptureLoop());
        Task.Run(SendLoopAsync); // 异步发送线程
    }

    void LateUpdate()
    {
        if (Camera.main != null && captureCamera != null)
        {
            captureCamera.transform.position = Camera.main.transform.position;
            captureCamera.transform.rotation = Camera.main.transform.rotation;
        }
    }

    IEnumerator CaptureLoop()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            captureCamera.Render();
            RenderTexture.active = renderTexture;
            tempTex.ReadPixels(new Rect(0, 0, 1440, 1080), 0, 0);
            tempTex.Apply();
            RenderTexture.active = null;

            byte[] jpgData = tempTex.EncodeToJPG(70);
            string base64Image = Convert.ToBase64String(jpgData);

            var frame = new FrameData
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                position = new Vector3Data(captureCamera.transform.position),
                rotation = new Vector3Data(captureCamera.transform.eulerAngles),
                image_base64 = base64Image
            };

            frameQueue.Enqueue(frame);
            yield return new WaitForSeconds(sendInterval);
        }
    }

    async Task SendLoopAsync()
    {
        while (true)
        {
            if (frameQueue.TryDequeue(out var frame))
            {
                try
                {
                    string json = JsonUtility.ToJson(frame);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync(serverUrl, content);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("⚠️ Send failed: " + e.Message);
                }
            }
            await Task.Delay(1);
        }
    }

    [Serializable]
    public class Vector3Data
    {
        public float x, y, z;
        public Vector3Data(Vector3 vec) { x = vec.x; y = vec.y; z = vec.z; }
    }

    [Serializable]
    public class FrameData
    {
        public long timestamp;
        public Vector3Data position;
        public Vector3Data rotation;
        public string image_base64;
    }
}
