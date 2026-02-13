using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using Dialogs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputsPlayer : CanvasPlayer
{
    [Header("Input Interaction")]
    [SerializeField] private GameObject CameraPlayer;
    [Tooltip("Capa de objetos interactuables")]
    [SerializeField] private LayerMask interactableMask;
    [Tooltip("Capa de NPCs")]
    [SerializeField] private LayerMask npcMask;
    [SerializeField] private bool isInteracting = false;
    [SerializeField] private GameObject NpcCop;
    [SerializeField] private GameObject NpcStealer;
    private bool haveBombsFalseSe = false;


    public bool IsInteracting { get => isInteracting; set => isInteracting = value; }

    public Action<bool> SetActiveCanvasObject;

    private void Start()
    {
        moneyText.text = MoneyCount.ToString();
        bombsText.text = BombsCount.ToString();
        SetActiveCanvasObject += (bool isActive) =>
        {
            if (isActive)
            {
                IsInteracting = false;
                EnableCanvas();
            }
            else
            {
                DisableCanvas();
            }
        };
        gameObject.GetComponent<PlayerConversant>().onOptionSelect += (DialogNode node, string iDParent) =>
        {
       
            if (iDParent == "BombDPoli")
            {
                if(node.GetBombsFalseSe()==true&& haveBombsFalseSe ==true && BombsCount == node.GetBombsCost())
                {
                    BombsCount -= node.GetBombsCost();

                    Debug.Log("Final malo");
                    return;
                }
                BombsCount -= node.GetBombsCost();
                Debug.Log("Final bueno");
            }
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isInteracting)
        {
            RayCasting();
        }
    }

    private void InteractWithInteractable(GameObject target)
    {
        if (target.TryGetComponent<InteractableObject>(out var interactable))
        {
            isInteracting = true;
            interactable.Interact(gameObject);
            Debug.Log("Interacting with InteractableObject."+ isInteracting);
            if(target.TryGetComponent<Moneda>(out var coin))
            {
                coin.OnCollected += OnCoinCollected;
                coin.Interact(gameObject); // pasa el jugador a la interacción
                return;
            }
            if(target.TryGetComponent<Bomb>(out var Bomb))
            {
                Bomb.OnCollectedBomb += OnBombCollected;
                Bomb.Interact(gameObject); // pasa el jugador a la interacción
                haveBombsFalseSe = true;
                return;
            }
        }
        else
        {
            Debug.LogWarning($"El objeto '{target.name}' no tiene componente InteractableObject.");
        }
    }
    private void OnCoinCollected(float amount)
    {
        MoneyCount += Mathf.RoundToInt(amount);
        // Actualiza UI del jugador
        moneyText.text = MoneyCount.ToString();
    }
    private void OnBombCollected(int amount)
    {
        BombsCount += amount;
        // Actualiza UI del jugador
        bombsText.text = BombsCount.ToString();
    }

    private void InteractWithNPC(GameObject target)
    {
        if (target.TryGetComponent<NPC>(out var npc))
        {
            isInteracting = true;
            npc.Talk(gameObject);
            Debug.Log("Interacting with NPC."+ isInteracting);
            if(target.GetComponent<NPC>().Npctype == "Cop")
            {
                NpcCop = target;
            }
            else if (target.GetComponent<NPC>().Npctype == "Stelear")
            {
                NpcStealer = target;
            }
        }
        else
        {
            Debug.LogWarning($"El objeto '{target.name}' no tiene componente NPC.");
        }
    }
    private void RayCasting()
    {
        Ray ray = new Ray(CameraPlayer.transform.position, CameraPlayer.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2f, interactableMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow, 2);
            InteractWithInteractable(hit.collider.gameObject);
        }
        else if (Physics.Raycast(ray, out hit, 2f, npcMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.cyan,2);
            InteractWithNPC(hit.collider.gameObject);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * 2f, Color.white,2);
            Debug.Log("No se ha detectado ningun objeto interactuable.");
        }
    }
}
