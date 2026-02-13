using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class InteractableObject : MonoBehaviour
{
    [SerializeField] protected float cuantity;
    protected bool isInteracted = false;
    [SerializeField] protected TextMeshProUGUI interactText;
    //protected virtual void Start()
    //{
    //    cuantity = 0;
    //}

    protected virtual void OnBeforeInteract() { }
    protected virtual void OnAfterInteract() { }

    public virtual void Interact(GameObject player)
    {
        Debug.Log($"Interacted with {gameObject.name}, Cuantity: {cuantity}");
        player.GetComponent<InputsPlayer>().IsInteracting = false;
        Debug.Log("InputsPlayer - IsInteracting set to false after interaction.");
    }

}