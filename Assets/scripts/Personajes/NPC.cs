using System;
using System.Collections;
using Dialogs;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public abstract class NPC : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private Dialog initialDialog;
    [SerializeField] private string npcName;
    [SerializeField] private string npctype;

    protected DialogueUI DialogueUI { get => dialogueUI; set => dialogueUI = value; }
    protected Dialog InitialDialog { get => initialDialog; set => initialDialog = value; }
    public string NpcName { get => npcName; set => npcName = value; }
    public string Npctype { get => npctype; set => npctype = value; }

    protected virtual void OnBeforeTalk(GameObject player) { }
    protected virtual void OnAfterTalk(GameObject player) { }

    public virtual void Talk(GameObject player)
    {
        //var conversant = player.GetComponent<PlayerConversant>();
        //Debug.Log("Hello, traveler! Welcome to our village.");

        //var fps = player.GetComponent<SC_FPSController>();
        //if (fps != null)
        //{
        //    // Asegúrate de que existe este método en SC_FPSController
        //    fps.DontMove();
        //    dialogueUI.gameObject.SetActive(true);
        //}
        //if (dialogueUI == null)
        //{
        //    Debug.LogError("DialogueUI no está asignado en el NPC.");
        //    return;
        //}
        //if (dialog == null)
        //{
        //    Debug.LogError("Dialog no está asignado en el NPC.");
        //    return;
        //}
        //if (conversant == null)
        //{
        //    Debug.LogError("PlayerConversant no está asignado en el jugador.");
        //    return;
        //}
        //conversant.GetDialogue(dialog);
        var conversant = player.GetComponent<PlayerConversant>();
        var fps = player.GetComponent<SC_FPSController>();

        if (dialogueUI == null) { Debug.LogError("DialogueUI no está asignado en el NPC."); return; }
        if (initialDialog == null) { Debug.LogError("Dialog no está asignado en el NPC."); return; }
        if (conversant == null) { Debug.LogError("PlayerConversant no está asignado en el jugador."); return; }

        OnBeforeTalk(player);

        //Debug.Log("Hello, traveler!");
        if (fps != null)
        {
            fps.DontMove();
            dialogueUI.gameObject.SetActive(true);
        }
        conversant.CurrentSpeakerNPC = npcName;
        //StartCoroutine(StartDialogueNextFrame(conversant, initialDialog));
    }
    protected IEnumerator StartDialogueNextFrame(PlayerConversant conversant,Dialog dialog)
    {
        yield return null; // espera 1 frame
        conversant.GetDialogue(dialog);
    }

}
