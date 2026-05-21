using UnityEngine;
using UnityEngine.UI;

/// <summary>설정 패널 — 닫기 버튼 및 패널 표시 제어.</summary>
public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
    }

    void OnEnable()
    {
        BindCloseButton();
    }

    void BindCloseButton()
    {
        if (closeButton == null) return;
        closeButton.onClick.RemoveListener(ClosePanel);
        closeButton.onClick.AddListener(ClosePanel);
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
