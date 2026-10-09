using System.Collections.Generic;
using UnityEngine;

public class PassiveEffectManager : MonoBehaviour
{
    private List<PassiveEffectBehaviour> activeEffects = new List<PassiveEffectBehaviour>();
    private List<GameObject> activeVisuals = new List<GameObject>();
    private Dictionary<ITickable, float> tickTimers = new Dictionary<ITickable, float>();

    private PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        Debug.Log($"[Manager] Awake. Player: {(player != null ? player.name : "NULO")}");
    }

    void Update()
    {
        if (DynamicMusicManager.IsFrozen)
            return;

        float delta = Time.deltaTime;

        // Efeitos contínuos
        for (int i = 0; i < activeEffects.Count; i++)
        {
            activeEffects[i].OnUpdate();
        }

        // Efeitos por tick
        if (tickTimers.Count > 0)
        {
            List<ITickable> tickableList = new List<ITickable>(tickTimers.Keys);
            foreach (var tickable in tickableList)
            {
                float timer = tickTimers[tickable] - delta;
                if (timer <= 0f)
                {
                    Debug.Log($"[Manager] Disparando tick para {tickable.GetType().Name}");
                    tickable.OnTick();
                    timer += tickable.TickInterval;
                    if (timer < 0f) timer = tickable.TickInterval;
                }
                tickTimers[tickable] = timer;
            }
        }
    }

    public void AddEffect(PassiveEffect data)
    {
        Debug.Log($"[Manager] AddEffect chamado. Data: {(data != null ? data.name : "NULO")}");

        if (data == null)
        {
            Debug.LogError("[Manager] AddEffect recebeu null.");
            return;
        }

        PassiveEffectBehaviour effect = data.CreateEffectInstance();
        if (effect == null)
        {
            Debug.LogError("[Manager] CreateEffectInstance retornou null.");
            return;
        }

        effect.OnEquip(player);
        activeEffects.Add(effect);
        Debug.Log($"[Manager] Efeito adicionado. Tipo: {effect.GetType().Name}");

        if (effect is ITickable tickable)
        {
            Debug.Log($"[Manager] Registrando tick. Intervalo: {tickable.TickInterval}s");
            tickTimers[tickable] = tickable.TickInterval;
        }
        else
        {
            Debug.Log("[Manager] Efeito contínuo (sem ticks).");
        }

        if (data.visualPrefab != null)
        {
            GameObject visual = Instantiate(data.visualPrefab, transform);
            activeVisuals.Add(visual);
        }
    }

    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects)
            effect.OnUnequip();
        activeEffects.Clear();
        tickTimers.Clear();
        foreach (var visual in activeVisuals)
            Destroy(visual);
        activeVisuals.Clear();
    }
}