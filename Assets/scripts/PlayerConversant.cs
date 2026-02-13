using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dialogs;
using UnityEngine;
using UnityEngine.Events;

public class PlayerConversant : MonoBehaviour
{
    [SerializeField] private string currentSpeakerPlayer = "Manueh";
    private string currentSpeakerNPC = "NPC";
    [SerializeField] Dialog testDialog;
    Dialog currentDialog;
    DialogNode currentNode = null;
    bool isChoosing = false;
    // [SerializeField] private Texture2D iconNPC;
    [SerializeField] private float testDelay = 0.2f;

    [SerializeField] public Action<DialogNode,string> onOptionSelect;
    public Action<bool> isTheLastNode;

    public float TestDelay { get => testDelay; set => testDelay = value; }
    public string CurrentSpeakerNPC { get => currentSpeakerNPC; set => currentSpeakerNPC = value; }
    //public Texture2D IconNPC { get => iconNPC; }

    public event Action OnConversationUpdated;

    //public void SetIconNPC(Texture2D newIcon)
    //{
    //    iconNPC = newIcon;
    //}

    private IEnumerator StartD(Dialog d)
    {

        yield return new WaitForSeconds(testDelay);
        StartDialogue(testDialog);
    }
    public void GetDialogue(Dialog newDialogue)
    {
        if (newDialogue == null)
        {
            Debug.LogWarning("No test dialogue assigned.");
        }
        Debug.Log("Starting test dialogue: " + newDialogue.name);
        testDialog = newDialogue;
        //StartCoroutine(StartD(testDialog));
        StartDialogue(testDialog);
    }

    public void StartDialogue(Dialog newDialogue)
    {
        if (newDialogue == null)
        {
            Debug.LogError("StartDialogue recibió un Dialog nulo.");
            return;
        }

        currentDialog = newDialogue;
        currentNode = currentDialog.GetRootNode();
        if (currentNode == null)
        {
            Debug.LogError($"El Dialog '{currentDialog.name}' no tiene RootNode.");
            return;
        }
        print("Starting dialogue: " + currentDialog);
        isTheLastNode?.Invoke(false);
        OnConversationUpdated?.Invoke();

    }

    public void Quit()
    {
        currentDialog = null;
        currentNode = null;
        isChoosing = false;
        gameObject.GetComponent<SC_FPSController>()?.CanMove();
        OnConversationUpdated?.Invoke();
        gameObject.GetComponent<InputsPlayer>()?.SetActiveCanvasObject?.Invoke(true);
    }

    public bool IsActive()
    {
        return currentDialog != null;
    }

    public bool IsChoosing()
    {
        return isChoosing;
    }

    internal string GetText()
    {
        if (currentDialog == null) return "";
        return currentNode.GetText();
    }

    public IEnumerable<DialogNode> GetChoices()
    {
        return currentDialog.GetPlayerChildren(currentNode);
    }

    public void SelectChoice(DialogNode chosenNode)
    {
        //Debug.Log("Player selected choice: " + chosenNode.name);
        currentNode = chosenNode;
        onOptionSelect.Invoke(currentNode,currentDialog.name);
        isChoosing = false;
        Next();
    }

    public void Next()
    {
        if (currentNode == null || currentDialog == null) return;

        var playerChildren = currentDialog.GetPlayerChildren(currentNode);
        if (playerChildren.Any())
        {
            isChoosing = true;
            OnConversationUpdated?.Invoke();
            return;
        }

        var aiChildren = currentDialog.GetAIChildren(currentNode).ToArray();
        if (aiChildren.Any())
        {
            int randomIndex = UnityEngine.Random.Range(0, aiChildren.Length);
            currentNode = aiChildren[randomIndex];
            OnConversationUpdated?.Invoke();

            // Evalúa el nodo NUEVO
            bool hasNext = currentDialog.GetPlayerChildren(currentNode).Any()
                        || currentDialog.GetAIChildren(currentNode).Any();

            Debug.Log($"Next() -> Nodo actual: {currentNode.name} | hasNext: {hasNext} | player:{currentDialog.GetPlayerChildren(currentNode).Count()} ai:{currentDialog.GetAIChildren(currentNode).Count()}");

            if (hasNext)
            {
                isTheLastNode?.Invoke(false);
            }
            else
            {
                isTheLastNode?.Invoke(true);
                //gameObject.GetComponent<InputsPlayer>()?.SetActiveCanvasObject?.Invoke(true);
            }
            return;
        }

        // Nodo terminal sin hijos de jugador ni de IA
        Debug.Log($"Next() -> Nodo terminal: {currentNode.name}");
        isTheLastNode?.Invoke(true);
        //gameObject.GetComponent<InputsPlayer>()?.SetActiveCanvasObject?.Invoke(true);
    }
    public bool HasNext()
    {
        //return currentDialog.GetAllChildren(currentNode).Count() > 0;
        //Debug.Log($"current node {currentNode}");
        ////if (currentNode == null) return false;
        ////return currentDialog.GetAllChildren(currentNode).Any();
        if (currentDialog == null || currentNode == null) return false;
        // Evita depender de GetAllChildren si está mal implementado
        bool hasPlayer = currentDialog.GetPlayerChildren(currentNode)?.Any() == true;
        bool hasAI = currentDialog.GetAIChildren(currentNode)?.Any() == true;
        return hasPlayer || hasAI;
    }

    public string GetCurrentSpeakerName()
    {
        if (isChoosing)
        {
            return currentSpeakerPlayer;
        }
        else
        {
            return currentSpeakerNPC;
        }
    }
}

