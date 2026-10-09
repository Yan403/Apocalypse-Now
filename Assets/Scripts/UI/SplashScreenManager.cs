using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;                            // se usar TextMeshPro
using UnityEngine.SceneManagement;

[System.Serializable]
public class SplashTextEntry
{
    [TextArea(1, 3)]
    public string text;                  // texto a ser exibido
    public float fadeInDuration = 1f;    // duração do fade in do texto
    public float showDuration = 2f;      // tempo que permanece totalmente visível
    public float fadeOutDuration = 1f;   // duração do fade out do texto
}

public class SplashScreenManager : MonoBehaviour
{
    [Header("Elementos da UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text displayText;     // use "Text" se for UI legada

    [Header("Configuração do Fundo")]
    [SerializeField] private float backgroundFadeInDuration = 1f;
    [SerializeField] private float backgroundFadeOutDuration = 1f;

    [Header("Textos Sequenciais")]
    [SerializeField] private List<SplashTextEntry> textEntries = new List<SplashTextEntry>();

    [Header("Próxima Cena")]
    [SerializeField] private string nextSceneName = "MainMenu";

    void Start()
    {
        StartCoroutine(PlaySplash());
    }

    private IEnumerator PlaySplash()
    {
        // 1. Fade in do fundo
        yield return StartCoroutine(FadeGraphic(backgroundImage, 0f, 1f, backgroundFadeInDuration));

        // 2. Exibe cada texto em sequência
        foreach (SplashTextEntry entry in textEntries)
        {
            // Define o texto
            displayText.text = entry.text;

            // Fade in do texto
            yield return StartCoroutine(FadeGraphic(displayText, 0f, 1f, entry.fadeInDuration));

            // Permanece visível
            yield return new WaitForSeconds(entry.showDuration);

            // Fade out do texto
            yield return StartCoroutine(FadeGraphic(displayText, 1f, 0f, entry.fadeOutDuration));

            // Pequena pausa entre textos (opcional)
            yield return new WaitForSeconds(0.1f);
        }

        // 3. Fade out do fundo
        yield return StartCoroutine(FadeGraphic(backgroundImage, 1f, 0f, backgroundFadeOutDuration));

        // 4. Carrega o menu
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Faz fade de alpha de um Graphic (Image, Text, etc.) de 'from' até 'to' em 'duration' segundos.
    /// </summary>
    private IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        float elapsed = 0f;
        Color color = graphic.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            graphic.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        graphic.color = new Color(color.r, color.g, color.b, to);
    }
}