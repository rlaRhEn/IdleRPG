using UnityEngine;
using UnityEngine.UI;

/// <summary>좌측 상단: 프로필 + 골드. UI 오브젝트는 하이어라키에 두고 여기에는 참조만 연결합니다.</summary>
public class PlayerTopLeftHudUI : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private Sprite profileSprite;

    [Header("연결된 UI")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Text goldText;

    void OnEnable()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged += OnProgressChanged;

        RefreshAll();
    }

    void OnDisable()
    {
        if (playerProgression != null)
            playerProgression.ExperienceChanged -= OnProgressChanged;
    }

    void OnProgressChanged() => RefreshGold();

    void RefreshAll()
    {
        if (playerProgression == null)
            playerProgression = GetComponent<PlayerProgression>();

        if (profileImage != null)
        {
            profileImage.sprite = profileSprite;
            profileImage.enabled = profileSprite != null;
        }

        RefreshGold();
    }

    void RefreshGold()
    {
        if (goldText == null) return;

        int gold = playerProgression != null ? playerProgression.CurrentGold : 0;
        goldText.text = $"Gold {gold}";
    }
}
