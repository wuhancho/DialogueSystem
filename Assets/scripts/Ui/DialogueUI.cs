using UnityEngine;
using Dialogs;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.Events;
using System;

public class DialogueUI : MonoBehaviour
{
    PlayerConversant playerConversant;
    [SerializeField] Image speakerImage;
    [SerializeField] TextMeshProUGUI AIText;
    [SerializeField] Button nextButton;
    [SerializeField] Button nextButton2;
    [SerializeField] GameObject CurrentSpeaker;
    [SerializeField] GameObject AIResponces;
    [SerializeField] Transform choicesRoot;
    [SerializeField] GameObject choicesPrefab;
    [SerializeField] Button quitButton;
    [SerializeField] UnityEvent onConversation;

    private TextMeshProUGUI speakerName;

    private string[] currentLines;
    private int currentLineIndex;
    private string currentNodeText;

    public Button NextButton { get => nextButton2; set => nextButton2 = value; }

    //private bool isBuildingText = false;

    public event Action OnDialogueEnd;

    private void Awake()
    {
        speakerName = CurrentSpeaker.GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        playerConversant = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerConversant>();
        playerConversant.OnConversationUpdated += UpdateUI;

        nextButton.onClick.AddListener(Next);
        quitButton.onClick.AddListener(QuitButton);

        playerConversant.isTheLastNode += (bool onAccion) =>
        {
            Debug.Log($"DialogueUI - isTheLastNode event triggered with value: {onAccion}");
            if (onAccion)
            {
                quitButton.gameObject.SetActive(true);
            }
        };
        //playerConversant.isTheLastNode += (bool onAccion) => OnDialogueFinalNode(onAccion);
        quitButton.gameObject.SetActive(false);
        UpdateUI();
    }

    void Next()
    {
        //playerConversant.Next();
        //if (currentLines != null && currentLineIndex < currentLines.Length - 1)
        //{
        //    currentLineIndex++;
        //    //BuildImageAI();
        //    BuildTextAI();
        //}
        if (playerConversant.HasNext())
        {
            playerConversant.Next();
        }
        else
        {
            Debug.Log("DialogueUI - Dialogue has ended.");  
            OnDialogueEnd?.Invoke();
        }
        

    }

    // Update is called once per frame
    void UpdateUI()
    {
        //Debug.Log("DialogueUI - Updating UI.");
        gameObject.SetActive(playerConversant.IsActive());
        if (!playerConversant.IsActive())
        {
            return;
        }
        speakerName.text = playerConversant.GetCurrentSpeakerName();
        //AIResponces.SetActive(!playerConversant.IsChoosing());
        nextButton.gameObject.SetActive(!playerConversant.IsChoosing());
        nextButton2.gameObject.SetActive(false);
        choicesRoot.gameObject.SetActive(playerConversant.IsChoosing());
        if (playerConversant.IsChoosing())
        {
            BuildChoiseList();
        }
        else
        {
            if (currentNodeText != playerConversant.GetText())
            {
                currentNodeText = playerConversant.GetText();
                if (currentNodeText.Contains('/'))
                    currentLines = currentNodeText.Split("/n");
                else
                    BuildTextAI(currentNodeText);
                currentLineIndex = 0;
            }

            //BuildImageAI();
            BuildTextAI();
        }
        //print("estado de player conversant last node" + playerConversant.isTheLastNode);
        
    }

    //private void BuildImageAI()
    //{
    //    speakerImage.sprite = Sprite.Create(playerConversant.IconNPC, new Rect(0, 0, playerConversant.IconNPC.width, playerConversant.IconNPC.height), new Vector2(0.5f, 0.5f));
    //    //Debug.Log($"DialogueUI - Building speaker image. the name at the image is {speakerImage.sprite.name}");
    //}

    //public void SetupSpeakerSprite(Sprite sprite)
    //{
    //    speakerImage.sprite = sprite;
    //}

    private void BuildTextAI()
    {
        //print($"hay texto? {currentLines} - {(currentLines != null ? currentLines.Length : -1)}");
        if (currentLines == null || currentLines.Length == 0) return;
        //print("yep");

        AIText.text = currentLines[currentLineIndex];
    }
    public void BuildTextAI(string text)
    {
        AIText.text = text;
    }

    private void BuildChoiseList()
    {
        choicesRoot.DetachChildren();
        foreach (DialogNode choice in playerConversant.GetChoices())
        {
            GameObject choiceInstance = Instantiate(choicesPrefab, choicesRoot);
            choiceInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.GetText();
            Button button = choiceInstance.GetComponentInChildren<Button>();
            button.onClick.AddListener(() =>
            {
                CurrentSpeaker.GetComponent<TextMeshProUGUI>().text = playerConversant.GetCurrentSpeakerName();
                playerConversant.SelectChoice(choice);
            });
        }
    }

    public void SetDialogueAllBoxVisible(bool isVisible)
    {
        CurrentSpeaker.SetActive(isVisible);
        AIResponces.SetActive(isVisible);
        //Debug.Log($"DialogueUI - {gameObject.GetComponent<Image>().name}");
        gameObject.GetComponent<Image>().enabled = isVisible;
    }
    public void SetDialogueSpeakerBoxVisible(bool isVisible)
    {
        CurrentSpeaker.SetActive(isVisible);
    }
    public void SetDialogueAIBoxVisible(bool isVisible)
    {
        nextButton.gameObject.SetActive(false);
        gameObject.GetComponent<Image>().enabled = isVisible;
        AIResponces.SetActive(isVisible);
    }

    public void SetDialogueQuitButtonVisible(bool isVisible)
    {
        quitButton.gameObject.SetActive(isVisible);
    }
    public void QuitButton()
    {
        playerConversant.Quit();
    }

    //public void OnDialogueFinalNode(bool onAccion)
    //{
    //    if (onAccion)
    //    {
    //        SetDialogueSpeakerBoxVisible(false);
    //        playerConversant.gameObject.GetComponent<SC_FPSController>().CanMove();
    //    }
    //    else
    //    {
    //        Debug.Log("DialogueUI - The final node is not an action node.");
    //    }
    //}

}
