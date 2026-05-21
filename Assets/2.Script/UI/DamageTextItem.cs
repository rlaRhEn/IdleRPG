using System;
using UnityEngine;

/// <summary>
/// 단일 데미지 텍스트 조각: 위로 떠오르며 페이드아웃 후 풀로 반환됩니다. <see cref="DamageTextPool"/> 전용입니다.
/// </summary>
public class DamageTextItem : MonoBehaviour
{
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float lifeTime = 0.7f;
    [SerializeField] private float baseCharacterSize = 0.055f;
    [SerializeField] private int baseFontSize = 64;

    private float elapsedTime;
    private Color baseColor;
    private Action<DamageTextItem> onFinished;
    private bool isPlaying;
    private float activeFloatSpeed;
    private float activeLifeTime;

    void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMesh>();
    }

    void Update()
    {
        if (!isPlaying) return;

        elapsedTime += Time.deltaTime;
        transform.position += Vector3.up * (activeFloatSpeed * Time.deltaTime);

        float t = Mathf.Clamp01(elapsedTime / activeLifeTime);
        Color color = baseColor;
        color.a = 1f - t;
        textMesh.color = color;

        if (elapsedTime >= activeLifeTime)
            Complete();
    }

    public void Play(
        string content,
        Vector3 position,
        Color color,
        Action<DamageTextItem> finishedCallback,
        float characterSizeScale = 1f,
        float floatSpeedScale = 1f,
        float lifeTimeScale = 1f)
    {
        if (textMesh == null) return;

        characterSizeScale = Mathf.Max(0.01f, characterSizeScale);
        floatSpeedScale = Mathf.Max(0.01f, floatSpeedScale);
        lifeTimeScale = Mathf.Max(0.01f, lifeTimeScale);

        transform.position = position;
        textMesh.text = content;
        textMesh.characterSize = baseCharacterSize * characterSizeScale;
        textMesh.fontSize = Mathf.RoundToInt(baseFontSize * characterSizeScale);
        textMesh.color = color;

        baseColor = color;
        elapsedTime = 0f;
        activeFloatSpeed = floatSpeed * floatSpeedScale;
        activeLifeTime = lifeTime * lifeTimeScale;
        onFinished = finishedCallback;
        isPlaying = true;
    }

    void Complete()
    {
        isPlaying = false;
        onFinished?.Invoke(this);
    }
}
