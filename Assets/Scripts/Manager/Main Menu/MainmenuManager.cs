using UnityEngine;
using UnityEngine.UI;

public class MainmenuManager : MonoBehaviour
{
    [Header("Start Screen Components")]
    [SerializeField] GameObject startScreenPanel;
    [SerializeField] Button startScreenButton;

    [Header("Main Menu Components")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] Button modeARButton;
    [SerializeField] Button modeLatihanButton;

    [Header("Setting Components")]
    [SerializeField] GameObject settingPanel;
    [SerializeField] Button settingButton;
    [SerializeField] Button closeSettingButton;

    [Header("Mode AR Components")]
    [SerializeField] GameObject modeARPanel;
    [SerializeField] Button modeARBackButton;
    [SerializeField] Button modeARStartButton;
    [SerializeField] string sceneARName;

    [Header("SFX Clip")]
    [SerializeField] AudioClip panelOpeningSFX;
    [SerializeField] AudioClip buttonClickSFX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartScreen();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartScreen()
    {
        startScreenButton.interactable = true;
        AudioManager.instance.PlaySound(panelOpeningSFX);
        LeanTween.moveLocalY(startScreenPanel, 0f, 1f).setEaseOutExpo();
    }

    public void MainMenu()
    {
        startScreenButton.interactable = false;
        AudioManager.instance.PlaySound(buttonClickSFX);
        LeanTween.moveLocalY(startScreenPanel, 1000f, 1f).setEaseInExpo().setOnComplete(() =>
        {
            startScreenPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
            LeanTween.moveLocalX(mainMenuPanel, 0f, 1f).setEaseOutExpo();
        });
    }

    public void OpenSetting()
    {
        settingButton.interactable = false;        
        AudioManager.instance.PlaySound(buttonClickSFX);
        AudioManager.instance.PlaySound(panelOpeningSFX);
        settingPanel.SetActive(true);
        LeanTween.moveLocalY(settingPanel, 0f, 1f).setEaseOutExpo().setOnComplete(() =>
        {
            // Setting panel is fully opened
            closeSettingButton.interactable = true;
        });
    }

    public void CloseSetting()
    {
        closeSettingButton.interactable = false;
        AudioManager.instance.PlaySound(buttonClickSFX);
        LeanTween.moveLocalY(settingPanel, -1000f, 1f).setEaseInExpo().setOnComplete(() =>
        {
            settingPanel.SetActive(false);
            settingButton.interactable = true;
        });
    }

    public void OpenModeAR()
    {
        modeARButton.interactable = false;
        AudioManager.instance.PlaySound(buttonClickSFX);
        LeanTween.moveLocalX(mainMenuPanel, 2000f, 1f).setEaseInExpo().setOnComplete(() =>
        {
            AudioManager.instance.PlaySound(panelOpeningSFX);
            mainMenuPanel.SetActive(false);
            modeARPanel.SetActive(true);
            LeanTween.moveLocalX(modeARPanel, 0f, 1f).setEaseOutExpo().setOnComplete(() =>
            {
                modeARBackButton.interactable = true;                
            });
        });
        
    }

    public void CloseModeAR()
    {
        modeARBackButton.interactable = false;        
        AudioManager.instance.PlaySound(buttonClickSFX);
        LeanTween.moveLocalX(modeARPanel, -2000f, 1f).setEaseInExpo().setOnComplete(() =>
        {
            modeARButton.interactable = true;
            AudioManager.instance.PlaySound(panelOpeningSFX);
            modeARPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
            LeanTween.moveLocalX(mainMenuPanel, 0f, 1f).setEaseOutExpo();
        });
    }
}
