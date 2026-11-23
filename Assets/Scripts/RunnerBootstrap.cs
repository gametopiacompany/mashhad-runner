using UnityEngine;

public sealed class RunnerBootstrap : MonoBehaviour
{
    private void Awake()
    {
        var existing = FindObjectOfType<RunnerGame>();
        if (existing != null)
        {
            return;
        }

        var game = new GameObject("Mashhad Runner Prototype");
        game.AddComponent<RunnerGame>();
    }
}
