using Dialogs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Stealer : NPC
{
    [SerializeField] private int bombsToCollect = 5;
    [SerializeField] private int bombsCollected = 0;
    [SerializeField] private Dialog GetAllBombsDialog;
    [SerializeField] private Dialog DontGetAllBombs;
    [SerializeField] private bool rewardGiven = false;
    [SerializeField] PlayerConversant playerConversant;

    protected override void OnBeforeTalk(GameObject player)
    {
        player.GetComponent<InputsPlayer>().DisableCanvas();
    }
    protected override void OnAfterTalk(GameObject player)
    {
        player.GetComponent<InputsPlayer>().EnableCanvas();
    }
    private void Start()
    {

        playerConversant.onOptionSelect += (DialogNode node, string iDParent) =>
        {
            if (iDParent == "BombDPoli")
            {
                if (node.GetBombsFalseSe() == true && bombsCollected >= node.GetBombsCost())
                {
                    bombsCollected -= node.GetBombsCost();
                    Debug.Log("Final malo");
                    SceneManager.LoadScene("Lose");
                    return;
                }
                bombsCollected -= node.GetBombsCost();
                Debug.Log("Final bueno");
                SceneManager.LoadScene("Win");
            }
        };
    }
    public override void Talk(GameObject player)
    {
        OnBeforeTalk(player);
        playerConversant = player.GetComponent<PlayerConversant>();
        bombsCollected = player.GetComponent<InputsPlayer>().BombsCount;
        if (HasAllBombs(bombsCollected))
        {
            if (playerConversant == null)
            {
                Debug.LogError("PlayerConversant no está asignado en el jugador.");
                return;
            }
            base.Talk(player);
            playerConversant.GetDialogue(GetAllBombsDialog);
        }
        else if (rewardGiven == false)
        {
            base.Talk(player);
            StartCoroutine(StartDialogueNextFrame(playerConversant, InitialDialog));
            rewardGiven = true;
        }
        else
        {
            base.Talk(player);
            playerConversant.GetDialogue(DontGetAllBombs);
        }
    }
    public void GetAllBombs()
    {
        Debug.Log("Cops: thank for help.");
    }
    public bool HasAllBombs(int Bombs)
    {
        return bombsCollected >= bombsToCollect;
    }
    public void CollectBomb()
    {
        bombsCollected++;
        Debug.Log($"Cops: Bombs collected {bombsCollected}/{bombsToCollect}");
    }
}
