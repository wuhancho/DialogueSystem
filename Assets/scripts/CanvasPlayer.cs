using System.Data.SqlTypes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CanvasPlayer : MonoBehaviour
{
    [Header("Canvas Objects")]
    [SerializeField] protected GameObject CanvasObjects;
    [SerializeField] protected TextMeshProUGUI moneyText;
    [SerializeField] protected TextMeshProUGUI bombsText;
    private float money;
    private int bombs;

    public float MoneyCount { get => money; set => money = value; }
    public int BombsCount { get => bombs; set => bombs = value; }
    public TextMeshProUGUI MoneyText { get => moneyText; set => moneyText = value; }
    public TextMeshProUGUI BombsText { get => bombsText; set => bombsText = value; }

    public void EnableCanvas()
    {
        CanvasObjects.SetActive(true);
    }
    public void DisableCanvas()
    {
        CanvasObjects.SetActive(false);
    }
}
