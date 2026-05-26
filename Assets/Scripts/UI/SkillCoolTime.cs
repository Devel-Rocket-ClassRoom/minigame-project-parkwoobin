using UnityEngine;
using UnityEngine.UI;

public class SkillCoolTime : MonoBehaviour
{
    public GameObject[] hideSkillButons;
    public Image[] hideSkillImages;
    [SerializeField] private float[] skillTimes = { 4f, 3f };

    private bool[] isHideSkills = { false, false };
    private float[] getskillTimes = { 0f, 0f };

    private void Start()
    {
        for (int i = 0; i < hideSkillButons.Length; i++)
            hideSkillButons[i].SetActive(false);
    }

    private void Update()
    {
        for (int i = 0; i < skillTimes.Length; i++)
        {
            if (!isHideSkills[i]) continue;

            getskillTimes[i] -= Time.deltaTime;
            hideSkillImages[i].fillAmount = Mathf.Max(0f, getskillTimes[i]) / skillTimes[i];

            if (getskillTimes[i] <= 0f) // 쿨다운 완료
            {
                getskillTimes[i] = 0f;
                isHideSkills[i] = false;    // 쿨다운 상태 해제
                hideSkillButons[i].SetActive(false);    // 버튼 숨기기
            }
        }
    }

    public void HideSkillSetting(int skillNum)
    {
        getskillTimes[skillNum] = skillTimes[skillNum];
        hideSkillImages[skillNum].fillAmount = 1f;
        hideSkillButons[skillNum].SetActive(true);
        isHideSkills[skillNum] = true;
    }

    public bool IsOnCooldown(int skillNum) => isHideSkills[skillNum];   // 외부에서 스킬이 현재 쿨다운 중인지 확인하는 메서드
    public float GetSkillTime(int skillNum) => skillTimes[skillNum];
}
