using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace MoriMonchiSimulator
{

public class CreatureVisualUI : MonoBehaviour
{
    [Header("Display references")]

    [SerializeField] private TextMeshProUGUI nameLabel;

    [SerializeField] private Image iconImage;

    [SerializeField] private TextMeshProUGUI stateLabel;

    public void Bind(CreatureDNA dna)
    {
        nameLabel.text = string.IsNullOrEmpty(dna.CustomName) ? dna.ToStringID() : dna.CustomName;

        MonchiPortraitUI.Apply(iconImage, dna);

        stateLabel.text = CreatureDisplay.StateOf(dna);
    }
}
}
