using System;
using UnityEngine;

public class Moneda : InteractableObject
{
    public Action<float> OnCollected;

    //protected override void Start()
    //{
    //    cuantity = 1;
    //}
    public override void Interact(GameObject p)
    {
        isInteracted = true;
        Debug.Log($"Moneda collected: {gameObject.name}");
        //cuantity += 1 + p.GetComponent<InputsPlayer>().MoneyCount;
        OnCollected?.Invoke(cuantity);
        base.Interact(p);
        OnAfterInteract();

    }
    protected override void OnAfterInteract()
    {
        Destroy(gameObject);
    }
}
