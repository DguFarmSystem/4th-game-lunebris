// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Linq;

public class StatAugmentation
{
    public string Grade { get; private set; }

    public float MinMoveSpeed { get; private set; }
    public float MaxMoveSpeed { get; private set; }

    public float MinAttackDamage { get; private set; }
    public float MaxAttackDamage { get; private set; }

    public float MinAttackSpeed { get; private set; }
    public float MaxAttackSpeed { get; private set; }

    public float MinSkillDamage { get; private set; }
    public float MaxSkillDamage { get; private set; }

    public float MinCoolDown { get; private set; }
    public float MaxCoolDown { get; private set; }

    public float MinDefensivePower { get; private set; }
    public float MaxDefensivePower { get; private set; }

    public float MinMagicDefensivePower { get; private set; }
    public float MaxMagicDefensivePower { get; private set; }

    public float MinMaxHp { get; private set; }
    public float MaxMaxHp { get; private set; }
}


[DisallowMultipleComponent]
public class AugmentationManager : MonoBehaviour
{
    [SerializeField] private Player.Player player;
    [SerializeField] private Button[] augmentationButtons;

    [Header("증강 확률")]
    [SerializeField] private int rareChance = 40;
    [SerializeField] private int epicChance = 30;
    [SerializeField] private int legendChance = 20;

    [SerializeField] private TextMeshProUGUI gradeTMP;

    private string[] grade = { "Rare", "Epic", "Legend" }; // Augementation Grade
    private string[] stats = { "MoveSpeed", "AttackDamage", "AttackSpeed", "SkillDamage", "CoolDown", "DefensivePower", "MagicDefensivePower", "MaxHp" };

    private int rareID = 0;
    private int epicID = 1;
    private int legendID = 2;

    private List<StatAugmentation> statAugmentations;

    private void Awake()
    {
        statAugmentations = GameManager.CSV.ReadCSVFile<List<StatAugmentation>>("StatAugmentation");
    }

    private void OnEnable()
    {
        LoadAugmentations();
    }

    private void LoadAugmentations()
    {
        List<string> randomList = GetRandomStats();
        string grade = GetRandomGrade();

        gradeTMP.text = grade;

        switch (grade)
        {
            case "Rare":
                for (int i = 0; i < randomList.Count; i++) AdaptAugmentation(rareID, randomList[i], augmentationButtons[i]);
                break;

            case "Epic":
                for (int i = 0; i < randomList.Count; i++) AdaptAugmentation(epicID, randomList[i], augmentationButtons[i]);
                break;

            case "Legend":
                for (int i = 0; i < randomList.Count; i++) AdaptAugmentation(legendID, randomList[i], augmentationButtons[i]);
                break;
        }
    }

    private string GetRandomGrade()
    {
        int chanceSum = rareChance + epicChance + legendChance;
        int random = Random.Range(1, chanceSum + 1);

        if (random < rareChance) return grade[rareID];
        else if (random < rareChance + epicChance) return grade[epicID];
        else return grade[legendID];
    }

    private List<string> GetRandomStats()
    {
        List<string> randomStat = new List<string>();

        // Shuffle
        for (int i = stats.Length - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            string temp = stats[i];
            stats[i] = stats[randIndex];
            stats[randIndex] = temp;
        }

        randomStat.Add(stats[0]);
        randomStat.Add(stats[1]);
        randomStat.Add(stats[2]);

        return randomStat;
    }

    private void AdaptAugmentation(int _gradeID, string _stat, Button _button)
    {
        TextMeshProUGUI[] tmps = _button.GetComponentsInChildren<TextMeshProUGUI>();
        Image[] images = _button.GetComponentsInChildren<Image>();

        TextMeshProUGUI nameTMP = tmps.First(tmp => tmp.name == "Name");
        TextMeshProUGUI descTMP = tmps.First(tmp => tmp.name == "Description");
        Image icon = images.First(image => image.name == "Icon");

        switch (_stat)
        {
            case "MoveSpeed":
                float randMoveSpeed = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinMoveSpeed, statAugmentations[_gradeID].MaxMoveSpeed));

                _button.onClick.AddListener(() => player.IncreaseMoveSpeed(randMoveSpeed));
                nameTMP.text = "속행";
                descTMP.text = "이동 속도 +" + randMoveSpeed.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "running");
                break;

            case "AttackDamage":
                float randAttackDamage = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinAttackDamage, statAugmentations[_gradeID].MaxAttackDamage));

                _button.onClick.AddListener(() => player.IncreaseAttackDamage(randAttackDamage));
                nameTMP.text = "무력";
                descTMP.text = "공격력 +" + randAttackDamage.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "swords");
                break;

            case "AttackSpeed":
                float randAttackSpeed = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinAttackSpeed, statAugmentations[_gradeID].MaxAttackSpeed));

                _button.onClick.AddListener(() => player.IncreaseAttackSpeed(randAttackSpeed));
                nameTMP.text = "속사";
                descTMP.text = "공격 속도 +" + randAttackSpeed.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "swords");
                break;

            case "SkillDamage":
                float randSkillDamage = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinSkillDamage, statAugmentations[_gradeID].MaxSkillDamage));

                _button.onClick.AddListener(() => player.IncreaseSkillDamage(randSkillDamage));
                nameTMP.text = "권능";
                descTMP.text = "스킬 데미지 +" + randSkillDamage.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "witchhat");
                break;

            case "CoolDown":
                float randCoolDown = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinCoolDown, statAugmentations[_gradeID].MaxCoolDown));

                _button.onClick.AddListener(() => player.IncreaseCoolDown(randCoolDown));
                nameTMP.text = "촉매";
                descTMP.text = "스킬 가속 +" + randCoolDown.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "hourglass");
                break;

            case "DefensivePower":
                float randDefensivePower = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinDefensivePower, statAugmentations[_gradeID].MaxDefensivePower));

                _button.onClick.AddListener(() => player.IncreaseDefensivePower(randDefensivePower));
                nameTMP.text = "철벽";
                descTMP.text = "방어력 +" + randDefensivePower.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "defence");
                break;

            case "MagicDefensivePower":
                float randMagicDefensivePower = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinMagicDefensivePower, statAugmentations[_gradeID].MaxMagicDefensivePower));

                _button.onClick.AddListener(() => player.IncreaseMagicDefensivePower(randMagicDefensivePower));
                nameTMP.text = "신념";
                descTMP.text = "마법 저항력 +" + randMagicDefensivePower.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "defence");
                break;

            case "MaxHp":
                float randMaxHp = Mathf.Ceil(Random.Range(statAugmentations[_gradeID].MinMaxHp, statAugmentations[_gradeID].MaxMaxHp));

                _button.onClick.AddListener(() => player.IncreaseMaxHp(randMaxHp));
                nameTMP.text = "활력";
                descTMP.text = "체력 +" + randMaxHp.ToString();
                icon.sprite = GameManager.Resource.Load<Sprite>("Sprites/Augment", "heart");
                break;
        }
    }
}
