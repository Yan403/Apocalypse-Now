using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField][Range(0f, 1f)] private float volume = 0.7f;

    private AudioSource audioSource;

    void Start()
    {
        // Adiciona um AudioSource se não existir
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configura o AudioSource
        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // Toca a música
        if (musicClip != null)
            audioSource.Play();
    }

    // Opcional: parar a música quando a cena for descarregada
    void OnDestroy()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}