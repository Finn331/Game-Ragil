using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderManager : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;

    private void Start()
    {
        // Set the slider values to the values stored in PlayerPrefs
        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat("musicVolume");
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (soundSlider != null)
        {
            soundSlider.value = PlayerPrefs.GetFloat("soundVolume");
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }
    }

    private void SetMusicVolume(float volume)
    {
        AudioManager.instance.SetMusicVolumeFromSlider(musicSlider);
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    private void SetSoundVolume(float volume)
    {
        AudioManager.instance.SetSoundVolumeFromSlider(soundSlider);
        PlayerPrefs.SetFloat("soundVolume", volume);
    }
}