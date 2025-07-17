// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

public class StatAugmentation
{
    private string Grade { get; set; }

    private string Category { get; set; }

    private float MinMoveSpeed { get; set; }
    private float MaxMoveSpeed { get; set; }

    private float MinAttackDamage { get; set; }
    private float MaxAttackDamage { get; set; }

    private float MinAttackSpeed { get; set; }
    private float MaxAttackSpeed { get; set; }

    private float MinSkillDamage { get; set; }
    private float MaxSkillDamage { get; set; }

    private float MinCoolDown { get; set; }
    private float MaxCoolDown { get; set; }

    private float MinDefensivePower { get; set; }
    private float MaxDefensivePower { get; set; }

    private float MinMagicDefensivePower { get; set; }
    private float MaxMagicDefensiePower { get; set; }

    private float MinMaxHp { get; set; }
    private float MaxMaxHp { get; set; }
}


[DisallowMultipleComponent]
public class Augmentation : MonoBehaviour
{
    [SerializeField] private Player.Player player;
    [SerializeField] private AugmentationButton[] augmentationButtons;

    [Header("Áõ°­ È®·ü")]
    [SerializeField] private int rareChance = 40;
    [SerializeField] private int epicChance = 30;
    [SerializeField] private int legendChance = 20;

    private string[] grade = { "Rare", "Epic", "Legend" }; // Augementation Grade
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
        foreach (AugmentationButton button in augmentationButtons)
        {
            string grade = GetRandomGrade();

            switch (grade)
            {
                case "Rare":

                    break;

                case "Epic":
                    break;

                case "Legend":
                    break;
            }
        }
    }

    private string GetRandomGrade()
    {
        int chanceSum = rareChance + epicChance + legendChance;
        int random = Random.Range(0, chanceSum + 1);

        if (random < rareChance) return grade[rareID];
        else if (random < epicChance) return grade[epicID];
        else return grade[legendID];
    }
}
