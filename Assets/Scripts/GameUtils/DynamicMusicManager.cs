using System.Collections;
using UnityEngine;

public class DynamicMusicManager : MonoBehaviour
{
    public static bool IsFrozen { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource normalMusicSource;
    [SerializeField] private AudioSource pursuitMusicSource;
    [SerializeField] private AudioSource alertAudioSource;

    [Header("Volumes (0 a 1)")]
    [SerializeField][Range(0f, 1f)] private float normalVolume = 0.8f;
    [SerializeField][Range(0f, 1f)] private float pursuitVolume = 0.8f;
    [SerializeField][Range(0f, 1f)] private float alertVolume = 1f;

    [Header("Alert Sound")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private float alertDuration = 2.5f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (normalMusicSource != null) normalMusicSource.playOnAwake = false;
        if (pursuitMusicSource != null) pursuitMusicSource.playOnAwake = false;
        if (alertAudioSource != null)
        {
            alertAudioSource.playOnAwake = false;
            alertAudioSource.ignoreListenerPause = true;
        }
    }

    private void OnEnable()
    {
        GlobalAlertManager.OnAlertActivated += HandleAlertActivated;
        GlobalAlertManager.OnAlertDeactivated += HandleAlertDeactivated;
    }

    private void OnDisable()
    {
        GlobalAlertManager.OnAlertActivated -= HandleAlertActivated;
        GlobalAlertManager.OnAlertDeactivated -= HandleAlertDeactivated;
    }

    private void Start()
    {
        PlayNormalMusic();
    }

    private void HandleAlertActivated()
    {
        StopAllCoroutines();
        currentRoutine = StartCoroutine(AlertSequence());
    }

    private void HandleAlertDeactivated()
    {
        StopAllCoroutines();
        Unfreeze();
        PlayNormalMusic();
    }

    private IEnumerator AlertSequence()
    {
        // 1. Pausa o jogo
        Freeze();

        // 2. Para a música atual
        if (normalMusicSource != null) normalMusicSource.Stop();
        if (pursuitMusicSource != null) pursuitMusicSource.Stop();

        // 3. Toca o alerta com o volume configurado
        if (alertClip != null && alertAudioSource != null)
        {
            alertAudioSource.clip = alertClip;
            alertAudioSource.loop = true;
            alertAudioSource.volume = alertVolume;          // 🆕 usa volume do Inspector
            alertAudioSource.Play();
        }

        yield return new WaitForSecondsRealtime(alertDuration);

        // 4. Para o alerta e despausa
        if (alertAudioSource != null) alertAudioSource.Stop();
        Unfreeze();

        // 5. Música de perseguição
        PlayPursuitMusic();
    }

    private void Freeze()
    {
        IsFrozen = true;
        Time.timeScale = 0f;
    }

    private void Unfreeze()
    {
        Time.timeScale = 1f;
        IsFrozen = false;
    }

    private void PlayNormalMusic()
    {
        if (normalMusicSource == null) return;

        if (pursuitMusicSource != null && pursuitMusicSource.isPlaying)
            pursuitMusicSource.Stop();

        normalMusicSource.Stop();
        normalMusicSource.volume = normalVolume;            // 🆕
        normalMusicSource.Play();
    }

    private void PlayPursuitMusic()
    {
        if (pursuitMusicSource == null) return;

        if (normalMusicSource != null && normalMusicSource.isPlaying)
            normalMusicSource.Stop();

        pursuitMusicSource.Stop();
        pursuitMusicSource.volume = pursuitVolume;          // 🆕
        pursuitMusicSource.Play();
    }
}