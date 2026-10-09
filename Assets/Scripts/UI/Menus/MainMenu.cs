using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Fade Visual")]
    [SerializeField] private Image fadeImage;                // Imagem preta que cobre a tela
    [SerializeField] private float fadeInDuration = 1.0f;    // Duração do fade in (entrada do menu)
    [SerializeField] private float fadeOutDuration = 1.0f;   // Duração do fade out (saída para o jogo)

    [Header("Áudio de Fundo (música ambiente)")]
    [SerializeField] private AudioSource musicAudioSource;

    [Header("Áudio de Start (tocado ao clicar em Jogar)")]
    [SerializeField] private AudioSource startAudioSource;
    [SerializeField] private AudioClip startClip;

    [Header("Créditos")]
    [SerializeField] private GameObject painelCreditos;

    void Start()
    {
        // Começa totalmente preto e faz fade in
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black; // alpha = 1 pela cor preta padrão
            StartCoroutine(FadeIn());
        }

        // Toca a música de fundo (se houver)
        if (musicAudioSource != null && musicAudioSource.clip != null)
            musicAudioSource.Play();
    }

    private IEnumerator FadeIn()
    {
        fadeImage.raycastTarget = true;   // garante que bloqueia durante o fade
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 0f);
        fadeImage.raycastTarget = false;  // libera cliques após o fade
    }

    public void Jogar()
    {
        // Para a música de fundo imediatamente
        if (musicAudioSource != null)
            musicAudioSource.Stop();

        // Toca o som de start
        if (startAudioSource != null && startClip != null)
        {
            startAudioSource.clip = startClip;
            startAudioSource.Play();
        }

        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        fadeImage.raycastTarget = true;  // volta a bloquear para o fade out
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            float initialStartVolume = startAudioSource != null ? startAudioSource.volume : 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;

                // Fade da tela (ficando preto)
                fadeImage.color = new Color(0, 0, 0, t);

                // Fade do áudio de start
                if (startAudioSource != null)
                    startAudioSource.volume = Mathf.Lerp(initialStartVolume, 0f, t);

                yield return null;
            }

            fadeImage.color = Color.black;
        }

        if (startAudioSource != null) startAudioSource.Stop();
        SceneManager.LoadScene("Level_1");
    }

    public void AbrirCreditos()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(true);
    }

    public void FecharCreditos()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(false);
    }
}