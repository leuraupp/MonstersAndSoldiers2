using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI soldiersRemainingText;
    [SerializeField] private Button attackButton;

    void Update() {
        soldiersRemainingText.text = $"{GridController.Instance.RemainingSoldiers} / {GridController.Instance.CurrentLevel.maxSoldiers}";
    }

    public void OnAttackButtonClick() {
        GameManager.Instance.ValidateAttack();
    }
}
