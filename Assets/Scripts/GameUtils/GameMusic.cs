using UnityEngine;

public class GameMusic : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField][Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource audioSource;

    void Start()
    {
        // Adiciona um AudioSource se não existir
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configuração básica
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // Inicia a reprodução
        if (musicClip != null)
        {
            if (fadeIn)
                StartCoroutine(FadeInRoutine());
            else
            {
                audioSource.volume = volume;
                audioSource.Play();
            }
        }
    }

    private System.Collections.IEnumerator FadeInRoutine()
    {
        audioSource.volume = 0f;
        audioSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;   // ignora Time.timeScale para fade suave mesmo com pausa
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = volume;
    }

    void OnDestroy()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}