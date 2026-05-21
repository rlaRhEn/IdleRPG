using System.Collections;

using UnityEngine;

using UnityEngine.UI;



/// <summary>오프라인 보상 팝업. 배경/Text는 HUD Canvas 하위에 두고 참조만 연결합니다.</summary>

public class OfflineRewardPopupUI : MonoBehaviour

{

    [SerializeField] private OfflineRewardManager offlineRewardManager;

    [SerializeField] private float popupDuration = 4f;

    [SerializeField] private Image popupBackground;

    [SerializeField] private Text popupText;



    Coroutine hideRoutine;



    void OnEnable()

    {

        if (offlineRewardManager == null)

            offlineRewardManager = GetComponent<OfflineRewardManager>();

        if (offlineRewardManager != null)

            offlineRewardManager.OfflineRewardApplied += OnOfflineRewardApplied;

    }



    void Start()

    {

        if (offlineRewardManager != null &&

            offlineRewardManager.TryConsumePendingReward(out int seconds, out int gold, out int exp))

            ShowPopup(seconds, gold, exp);

        else if (popupBackground != null)

            popupBackground.gameObject.SetActive(false);

    }



    void OnDisable()

    {

        if (offlineRewardManager != null)

            offlineRewardManager.OfflineRewardApplied -= OnOfflineRewardApplied;

        if (hideRoutine != null)

        {

            StopCoroutine(hideRoutine);

            hideRoutine = null;

        }

    }



    void OnOfflineRewardApplied(int offlineSeconds, int rewardGold, int rewardExperience)

    {

        ShowPopup(offlineSeconds, rewardGold, rewardExperience);

    }



    void ShowPopup(int offlineSeconds, int rewardGold, int rewardExperience)

    {

        if (popupBackground == null || popupText == null) return;



        int minutes = offlineSeconds / 60;

        int seconds = offlineSeconds % 60;

        popupText.text =

            "Offline Reward\n" +

            $"Time: {minutes:D2}:{seconds:D2}\n" +

            $"+Gold {rewardGold}   +EXP {rewardExperience}";



        popupBackground.gameObject.SetActive(true);



        if (hideRoutine != null)

            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());

    }



    IEnumerator HideAfterDelay()

    {

        yield return new WaitForSeconds(Mathf.Max(0.5f, popupDuration));

        if (popupBackground != null)

            popupBackground.gameObject.SetActive(false);

        hideRoutine = null;

    }

}

