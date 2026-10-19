using UnityEngine;

/// Objeto con el que se interactúa con E (puertas). NO es una anomalía: usarlo nunca cuenta como error.
public abstract class Interactable : MonoBehaviour
{
    public abstract string Prompt { get; }
    public abstract void Interact(GameManager game);
}
