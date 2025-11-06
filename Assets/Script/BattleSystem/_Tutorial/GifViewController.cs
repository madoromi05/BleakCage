using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// gifの表示管理クラス
/// </summary>
public class　GifViewController : MonoBehaviour
{
    private RawImage rawImage;
    private List<UniGif.GifTexture> gifTextures;
    private float gifTimer = 0f;
    private int gifFrameIndex = 0;
    private bool isPlaying = false;

    public void Initialize(RawImage targetImage)
    {
        rawImage = targetImage;
        rawImage.gameObject.SetActive(false);
    }

    public IEnumerator LoadAndPlayGif(string gifFileName)
    {
        StopGif();
        rawImage.gameObject.SetActive(true);

        var path = Path.Combine(Application.streamingAssetsPath, gifFileName);
        if (!File.Exists(path))
        {
            rawImage.gameObject.SetActive(false);
            yield break;
        }

        byte[] file = File.ReadAllBytes(path);

        yield return StartCoroutine(UniGif.GetTextureListCoroutine(file, (gifTexList, loopCount, width, height) =>
        {
            if (gifTexList != null && gifTexList.Count > 0)
            {
                gifTextures = gifTexList;
                rawImage.texture = gifTextures[0].m_texture2d;
                rawImage.rectTransform.sizeDelta = new Vector2(width, height);
                isPlaying = true;
            }
            else
            {
                Debug.LogError("Failed to load GIF textures.");
                rawImage.gameObject.SetActive(false);
            }
        }));
    }

    public void StopGif()
    {
        isPlaying = false;
        gifTimer = 0f;
        gifFrameIndex = 0;
        rawImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPlaying && gifTextures != null && gifTextures.Count > 0)
        {
            gifTimer += Time.deltaTime;
            if (gifTimer >= gifTextures[gifFrameIndex].m_delaySec)
            {
                gifTimer = 0f;
                gifFrameIndex = (gifFrameIndex + 1) % gifTextures.Count;
                rawImage.texture = gifTextures[gifFrameIndex].m_texture2d;
            }
        }
    }
}