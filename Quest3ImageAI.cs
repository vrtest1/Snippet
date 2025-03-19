using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using Newtonsoft.Json;

public class Quest3ImageAI : MonoBehaviour
{
    public RenderTexture displayImage;
    private WebCamTexture webCamTexture;
    private bool CapState = false;
    [SerializeField] private GameObject targetObj;
    [SerializeField] private Material renderMaterial;

    [Header("API Settings")]
    [SerializeField] private string apiKey = "YOUR_OPENAI_API_KEY";
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private int targetWidth = 640;
    [SerializeField] private int targetHeight = 480;
    [SerializeField] private int maxTokens = 300;
    [SerializeField] private float requestInterval = 3.0f;
    [SerializeField] private float minProcessTime = 1.0f; // 最低処理時間

    private bool isProcessing = false;
    private float lastRequestTime;

    [SerializeField]
    private string defaultAsking, receipeAsking;

    private bool recipemode = false;

    [SerializeField]
    private GameObject recipesign;

    private bool bootCamera = false;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name, 1280, 960);
            webCamTexture.Play();
        }

        Renderer renderer = targetObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderMaterial = renderer.material;
        }
    }

    private void Update()
    {
        if (webCamTexture.didUpdateThisFrame)
        {
            Graphics.Blit(webCamTexture, displayImage);
        }

        //初回カメラが間に合ってない？のでもう一回Play()
        if (!bootCamera)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                webCamTexture.Play();
            }

            if (OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger))
            {
                bootCamera = true;
            }
        }

        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) && Time.time - lastRequestTime > requestInterval && bootCamera)
        {
            if (!isProcessing)
            {
                CapState = !CapState;

                if (CapState)
                {
                    StopAllCoroutines();
                    StartCoroutine(CaptureAndAnalyze());
                    webCamTexture.Pause();
                    targetObj.transform.localScale = new Vector3(2, 2, 2);
                    responseText.text = "分析中...";
                }
                else
                {
                    webCamTexture.Play();
                    targetObj.transform.localScale = new Vector3(1, 1, 1);
                    if (!isProcessing)
                    {
                        responseText.text = "";
                    }
                }
            }
        }

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            //Debug.Log("Aボタンを押した");
            recipemode = !recipemode;

            if (recipemode)
            {
                recipesign.SetActive(true);
            }
            else
            {
                recipesign.SetActive(false);
            }
        }
    }

    private IEnumerator CaptureAndAnalyze()
    {
        if (isProcessing) yield break;
        isProcessing = true;
        lastRequestTime = Time.time;
        float startTime = Time.time;

        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        RenderTexture renderTexture = RenderTexture.GetTemporary(targetWidth, targetHeight, 24);

        Graphics.Blit(webCamTexture, renderTexture);
        RenderTexture.active = renderTexture;
        resizedTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resizedTexture.Apply();

        RenderTexture.ReleaseTemporary(renderTexture);

        byte[] imageBytes = resizedTexture.EncodeToJPG(75);
        string base64Image = Convert.ToBase64String(imageBytes);
        Destroy(resizedTexture);

        Debug.Log($"Image Size: {imageBytes.Length / 1024}KB");

        if (!recipemode)
        {
            yield return StartCoroutine(SendToChatGPT(base64Image, defaultAsking));
        }
        else
        {
            yield return StartCoroutine(SendToChatGPT(base64Image, receipeAsking));
        }

        // 最低処理時間の保証
        float elapsed = Time.time - startTime;
        if (elapsed < minProcessTime)
        {
            yield return new WaitForSeconds(minProcessTime - elapsed);
        }

        isProcessing = false;
    }

    private IEnumerator SendToChatGPT(string imageBase64, string prompt)
    {
        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{imageBase64}" }
                        }
                    }
                }
            },
            max_tokens = maxTokens
        };

        string json = JsonConvert.SerializeObject(requestBody);
        Debug.Log($"Request JSON:\n{json}");

        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(data);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"API Response:\n{www.downloadHandler.text}");
                var response = JsonConvert.DeserializeObject<OpenAIResponse>(www.downloadHandler.text);
                if (response != null && response.choices != null && response.choices.Count > 0)
                {
                    if (CapState && isProcessing)
                    {
                        responseText.text = response.choices[0].message.content;
                    }
                }
                else
                {
                    responseText.text = "有効なレスポンスがありませんでした";
                }
            }
            else
            {
                string errorMessage = $"エラー: {www.error}\nレスポンス: {www.downloadHandler.text}";
                Debug.LogError(errorMessage);
                responseText.text = errorMessage;
            }
        }
    }

    [System.Serializable]
    private class OpenAIResponse
    {
        public List<Choice> choices { get; set; }
    }

    [System.Serializable]
    private class Choice
    {
        public Message message { get; set; }
    }

    [System.Serializable]
    private class Message
    {
        public string content { get; set; }
    }

    void OnDestroy()
    {
        webCamTexture?.Stop();
    }
}
