using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Dialogs
{
    public class PlayerConversant : MonoBehaviour
    {
        private string currentSpeakerPlayer = "Padre Cael";
        private string currentSpeakerNPC = "NPC";
        [SerializeField] Dialog testDialog;
        Dialog currentDialog;
        DialogNode currentNode = null;
        bool isChoosing = false;
        [SerializeField] private Texture2D iconNPC;
        [SerializeField] private float testDelay = 2;

        [SerializeField] public UnityEvent<DialogNode> onOptionSelect;
        [SerializeField] public Action<bool> isTheLastNode;

        public float TestDelay { get => testDelay; set => testDelay = value; }
        public string CurrentSpeakerNPC { get => currentSpeakerNPC; set => currentSpeakerNPC = value; }
        public Texture2D IconNPC { get => iconNPC; }

        public event Action OnConversationUpdated;

        public void SetIconNPC(Texture2D newIcon)
        {
            iconNPC = newIcon;
        }

        private IEnumerator StartD(Dialog d)
        {

            yield return new WaitForSeconds(testDelay);
            StartDialogue(testDialog);
        }
        public void GetTestDialogue(Dialog newDialogue)
        {
            if (newDialogue == null) {
                Debug.LogWarning("No test dialogue assigned.");
            }
            Debug.Log("Starting test dialogue: " + newDialogue.name);
            testDialog = newDialogue;
            StartCoroutine(StartD(testDialog));
            //StartDialogue(testDialog);
        }

        public void StartDialogue(Dialog newDialogue)
        {
            currentDialog = newDialogue;
            currentNode = currentDialog.GetRootNode();
            isTheLastNode.Invoke(false);
            OnConversationUpdated?.Invoke();
        }

        public void Quit()
        {
            currentDialog = null;
            currentNode = null;
            isChoosing = false;
            OnConversationUpdated?.Invoke();
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
            onOptionSelect.Invoke(currentNode);
            isChoosing = false;
            Next();
        }

        public void Next()
        {
            //int numPlayerResponses = currentDialog.GetPlayerChildren(currentNode).Count();
            //if(numPlayerResponses > 0)
            //{
            //    isChoosing = true;
            //    OnConversationUpdated?.Invoke();
            //    return;
            //}

            //DialogNode[] children = currentDialog.GetAIChildren(currentNode).ToArray();
            //int randomIndex = UnityEngine.Random.Range(0, children.Count());
            //currentNode = children[randomIndex];
            //OnConversationUpdated?.Invoke();
            if (currentNode == null) return;

            // Comprobar si hay opciones para el jugador
            var playerChildren = currentDialog.GetPlayerChildren(currentNode);
            if (playerChildren.Any())
            {
                isChoosing = true;
                OnConversationUpdated?.Invoke();
                return;
            }

            // Si no, buscar respuesta de la IA
            var aiChildren = currentDialog.GetAIChildren(currentNode).ToArray();
            if (aiChildren.Any())
            {
                int randomIndex = UnityEngine.Random.Range(0, aiChildren.Length);
                currentNode = aiChildren[randomIndex];
                OnConversationUpdated?.Invoke();
                if (HasNext())
                {
                    //Debug.Log($"Reached the end of the dialogue, The value of isTheLast: {isTheLastNode}");
                    isTheLastNode.Invoke(false);
                }
                else
                {
                    //Debug.Log($"Reached the end of the dialogue, The value of isTheLast: {isTheLastNode}");
                    isTheLastNode.Invoke(true);
                }
            }
        }
        public bool HasNext()
        {
            //return currentDialog.GetAllChildren(currentNode).Count() > 0;
            if (currentNode == null) return false;
            return currentDialog.GetAllChildren(currentNode).Any();
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
} 

