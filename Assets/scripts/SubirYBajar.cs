using UnityEngine;

public class SubirYBajar : MonoBehaviour
{
    public float altura = 1f;      // Qué tan alto sube
    public float velocidad = 1f;   // Qué tan rápido se mueve

    private Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        float movimiento = Mathf.Sin(Time.time * velocidad) * altura;
        transform.position = posicionInicial + Vector3.up * movimiento;
    }
}
