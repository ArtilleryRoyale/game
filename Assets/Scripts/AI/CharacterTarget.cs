using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jrmgx.Helpers;
using UnityEngine;

public enum CharacterTargetTag : int { Human = 0, AI = 1 }

public class CharacterTarget : MonoBehaviour, ExplosionReceiver
{
    /// This time is based on the real physics engine
    /// not the AI simulation time (which can be accelerated sometime)
    private const float SimulationTime = 5f;

    // State
    public CharacterTargetTag TargetTag { get; set; }
    private bool hasTimeout;
    private float initStarted;
    private List<CharacterTargetHit> hits = new List<CharacterTargetHit>();

    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        // Log.Message("AI Simulation", "OnReceiveExplosion Target " + name);
        hits.Add(new CharacterTargetHit(TargetTag, explosion, damages));
    }

    public async UniTask<List<CharacterTargetHit>> WaitForAISimulation()
    {
        // Log.Message("AI Simulation", "WaitForAISimulation started at: " + Time.fixedTime);
        initStarted = Time.fixedTime;
        await UniTask.WaitUntil(() => hasTimeout).CancelOnDestroy(this);
        return hits;
    }

    private void FixedUpdate()
    {
        if (initStarted == 0) return;
        if (hasTimeout == true) return;
        if (initStarted + SimulationTime < Time.fixedTime) {
            // Log.Message("AI Simulation", "WaitForAISimulation timed out at: " + Time.fixedTime);
            hasTimeout = true;
        }
    }
}
