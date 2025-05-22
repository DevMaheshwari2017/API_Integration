using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class Dictionary : MonoBehaviour
{
    [SerializeField] private TMP_InputField wordInputField;
    [SerializeField] private Button getMeaningBtn;
    [SerializeField] private TextMeshProUGUI definitionText;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject noAudioErrorMessage;

    void Start()
    {
        noAudioErrorMessage.SetActive(false);
        getMeaningBtn.onClick.AddListener(OnSubmitWord);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// On Get meaning btn, we checks if the input word conatins a space and shows a appropiate message
    /// After checking make the input lower case, as api is case sensitive and start the coroutine
    /// </summary>
    private void OnSubmitWord()
    {
        string input = wordInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(input) || input.Contains(" "))
        {
            definitionText.text = "Please enter a single word without spaces.";
            return;
        }

        StartCoroutine(GetMeaning(input.ToLower()));
    }

    /// <summary>
    /// Using the word getting full url, on failuer shows error.
    /// Using Newtonsoft.Json to DeserializeObject json file and get C# appropiate data.
    /// Take only the first defination, pass it to defination text and also get audio url from phonetics.
    /// </summary>
    /// <param name="_word"></param>
    /// <returns></returns>
    private IEnumerator GetMeaning(string _word) 
    {
        string url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{_word}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            definitionText.text = "Word not found or API error.";
            yield break;
        }

        var json = request.downloadHandler.text;
        DictionaryEntry[] parsed = JsonConvert.DeserializeObject<DictionaryEntry[]>(json);

        if (parsed == null || parsed.Length == 0)
        {
            definitionText.text = "No definition found.";
            yield break;
        }

        var first = parsed[0];

        string definition = first.meanings?[0]?.definitions?[0]?.definition;
        if (string.IsNullOrEmpty(definition))
        {
            definitionText.text = "Definition missing.";
            yield break;
        }

        definitionText.text = definition;

        string audioUrl = first.phonetics?[0]?.audio;
        if (!string.IsNullOrEmpty(audioUrl))
        {
            StartCoroutine(PlayPronunciationAudio(audioUrl));
        }
    }

    /// <summary>
    /// Play Prounciation audio using the audio url from phonetics - The API provided a url for audio also
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private IEnumerator PlayPronunciationAudio(string url)
    {
        // get audio clip with type MPEG
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Play audio clip and also the animation till the audio clip lentgh
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            audioSource.clip = clip;
            characterAnimator.SetBool("IsTalking", true);
            audioSource.Play();
            yield return new WaitForSeconds(clip.length);
            characterAnimator.SetBool("IsTalking", false);
        }
        else 
        {
            noAudioErrorMessage.SetActive(true);
            yield return new WaitForSeconds(2f);
            noAudioErrorMessage.SetActive(false);
        }
    }
}

/// <summary>
/// Classes that conatins C# appropiate data after Deserialization of JSON
/// </summary>
[Serializable]
public class DictionaryEntry
{
    public string word;
    public Phonetic[] phonetics;
    public Meaning[] meanings;
}

[Serializable]
public class Phonetic
{
    public string audio;
}

[Serializable]
public class Meaning
{
    public Definition[] definitions;
}

[Serializable]
public class Definition
{
    public string definition;
}