using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager Instance { get; set; }
    private SaveManager saveManager;
    private ToolManager toolManager;
    private UIManager uiManager;
    private NoteManager noteManager;
    private AudioManager audioManager;
    public SaveFile SaveFile;
    
    private CustomCursor cursor;
    private Timeline timeLine;
    CustomPopup overwriteConfirmationPopup;
    
    private bool isStopWhatPlayerIsDoing = false;
    private bool playerWantOverwritePopup = true;
    
    [Header("Buttons")]
    [SerializeField] private List<Button> legacyButtonsTools = new List<Button>();
    [SerializeField] private List<Button> legacyButtonsTimeline = new List<Button>();
    [SerializeField] private List<Button> legacyButtonSaving = new List<Button>();
    [SerializeField] private TMP_InputField saveFileInputField;
    [SerializeField] private GameObject overwriteIndicator;
    [SerializeField] private GameObject loopTimelineIndicator;
    
    [Header("Popup")]
    [SerializeField] private GameObject popUp;
    
    [Header("Cursors")]
    [SerializeField] private SpriteRenderer cursorImageRenderer;
    [SerializeField] private List<Sprite> cursorIcons = new List<Sprite>();

    [Header("Timeline")] 
    [SerializeField] private Slider timeLineSlider;
    
    [Header("notes")]
    [SerializeField] private GameObject notePrefab = null;
    [SerializeField] private Transform allNotesParents = null;

    [Header("Audio")] 
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        InitializeManagers();
        SetCurrentSelectedTool(0);
        InitializeCustomButtons();
        
        saveManager.AddSaveable(timeLine);
        saveManager.AddSaveable(noteManager);
    }

    private void OnDisable()
    {
        //want anders word valentijn boos
        timeLine?.RemoveListener();
        noteManager?.RemoveListeners();
        uiManager?.RemoveListeners();
    }
    
    private void InitializeManagers()
    {
        Instance = this;
        SaveFile = new SaveFile();
        timeLine = new Timeline(Instance);
        
        saveManager = new SaveManager(Instance);
        audioManager = new AudioManager(audioSource);
        toolManager = new ToolManager();
        noteManager = new NoteManager(Instance, audioManager, notePrefab, allNotesParents);
        uiManager = new UIManager(Instance ,overwriteIndicator, loopTimelineIndicator ,timeLineSlider);
        cursor = new CustomCursor(cursorImageRenderer);
        overwriteConfirmationPopup = new CustomPopup(popUp, Instance);
    }
    
    private void InitializeCustomButtons()
    {
        uiManager.InitializeToolButtons(legacyButtonsTools, SetCurrentSelectedTool);
        uiManager.InitializeTimelineButtons(legacyButtonsTimeline, SetTimeline);
        uiManager.InitializeSavingButtons(legacyButtonSaving, SaveOrLoad);
    }

    private void Update()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursor.UpdateCursorPosition(mouseWorldPosition);

        if (isStopWhatPlayerIsDoing) return;
        
        if (toolManager?.GetSelectedTool() == 1 && Input.GetMouseButton(0))
            noteManager?.PlaceOrRemoveNoteAtMousePosition(mouseWorldPosition, true);
        
        if (toolManager?.GetSelectedTool() == 2 && Input.GetMouseButton(0))
            noteManager?.PlaceOrRemoveNoteAtMousePosition(mouseWorldPosition, false);
    }

    private void SetCurrentSelectedTool(int _toolIndex)
    {
        if (isStopWhatPlayerIsDoing) return;
        Cursor.visible = _toolIndex == 0;
        cursor.ChangeCursorImage(cursorIcons[_toolIndex]);
        toolManager?.SetCurrentSelectedTool(_toolIndex);
    }

    private void SetTimeline(int _timelineIndex)
    {
        if (isStopWhatPlayerIsDoing) return;
        if (timeLine == null) return;
        switch (_timelineIndex)
        {
            case 0: timeLine.StartTimeline(); break;
            case 1: timeLine.PauseTimeline(); break;
            case 2: timeLine.StopTimeline(); break;
            case 3: timeLine.ToggleRepeatTimeline(); uiManager.ToggleLoopIndicator(); break;
            default: Debug.LogWarning("Unknown timeline index: " + _timelineIndex); break;
        }
    }

    private void SaveOrLoad(int _saveIndex)
    {
        if (isStopWhatPlayerIsDoing) return;
        switch (_saveIndex)
        {
            case 0: 
                saveManager.SaveTool(saveFileInputField.text); 
                break;
            case 1: 
                saveManager.LoadTool(saveFileInputField.text); 
                break;
            case 2:
                playerWantOverwritePopup = !playerWantOverwritePopup;
                uiManager.ToggleOverwriteIndicator(); 
                break;
            case 3: noteManager.ClearAllNotes();
                saveFileInputField.text = "";
                break;
            default: Debug.LogWarning("Unknown save index: " + _saveIndex); break;
        }
    }

    public void TogglePlayerStopDoing()
    {
        isStopWhatPlayerIsDoing = !isStopWhatPlayerIsDoing;
    }
    
    public void HandleOverwriteConfirmation(string _fileName)
    {
        if (!playerWantOverwritePopup) return;
        
        saveFileInputField.interactable = false;
        SetCurrentSelectedTool(0);
        TogglePlayerStopDoing();
        overwriteConfirmationPopup.ShowConfirmationPopup(
            () =>
            {
                TogglePlayerStopDoing();
                saveFileInputField.interactable = true;
                saveManager.OverwriteSaveFile(_fileName);
            },
            () =>
            {
                saveFileInputField.interactable = true;
                TogglePlayerStopDoing();
            }
        );
    }
}