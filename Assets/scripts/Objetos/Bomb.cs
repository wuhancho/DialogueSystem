using System;
using UnityEngine;

public class Bomb : InteractableObject
{
    public Action<int> OnCollectedBomb;


    //protected override void Start()
    //{
    //    cuantity = 0;
    //}
    public override void Interact(GameObject p)
    {
        Debug.Log($"Bombs: {gameObject.name}");
        //cuantity += 1 + p.GetComponent<InputsPlayer>().BombsCount;
        OnCollectedBomb?.Invoke((int)cuantity);
        base.Interact(p);
        OnAfterInteract();
    }
    protected override void OnAfterInteract()
    {
        Destroy(gameObject);
    }
}
