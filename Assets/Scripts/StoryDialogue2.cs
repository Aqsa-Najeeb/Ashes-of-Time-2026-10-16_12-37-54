/*
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class StoryDialogue2 : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    private string[] dialogueLines =
    {
        // SCENE 1: He enters the house
        
        "NARRATOR: He follows the shadow through the dark streets until it enters an abandoned house.",
        "CHARACTER: Hey! Wait for me!",
        "NARRATOR: He enters the house, and suddenly the door slams shut.",
        "CHARACTER: What?! The door is locked! How am I supposed to get out?",
        "NARRATOR: He tries opening it, but it won't move.",

        // SCENE 2: Finding the key
        
        "NARRATOR: He notices a small table in the corner. On it lies a note.",
        "[NOTE] The key is hidden where darkness never reaches.",
        "NARRATOR: The player looks around and notices a small light coming through a broken window.",
        "Under the light, he finds a key.",
        "CHARACTER: Finally! Let's get out of here.",
        "NARRATOR: As he picks up the key, he sees the shadow monster standing behind him.",
        "CHARACTER: Oh no...",
        "NARRATOR: The monster disappears, and he runs toward the door.",

        // SCENE 3: The final escape
        "[SCENE 3: THE FINAL ESCAPE]",
        "He unlocks the door, but the hallway outside has become completely dark.",
        "The monster appears at the other end and starts running toward him.",
        "NARRATOR: He rushes outside just as the creature reaches the doorway.",
        "CHARACTER: What was that thing?! Why is it following me?",
        "NARRATOR: He runs away from the house. Behind him, the door slowly closes by itself.",
        "NARRATOR: He thinks he has escaped, but he doesn't notice the black shadow following him on the ground.",
        "[CUT TO BLACK]"
    };

    private int currentLine = 0;
    private int startFrame = -1;

    private bool dialogueStarted = false;

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    public void BeginDialogue()
    {
        currentLine = 0;
        dialogueStarted = true;
        startFrame = Time.frameCount;
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    void Update()
    {
        if (!dialogueStarted || Time.frameCount == startFrame)
            return;
       

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            currentLine++;

            if (currentLine < dialogueLines.Length)
            {
                ShowCurrentLine();
            }
            else
            {
                dialoguePanel.SetActive(false);
                dialogueStarted = false;
            }
        }
    }

    void ShowCurrentLine()
    {
        dialogueText.text = dialogueLines[currentLine];
    }

    
    public void ShowFirstLine()
    {
        currentLine = 0;
        ShowCurrentLine();
    }
}*/
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class StoryDialogue2 : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    // Optional: hook up a cut to black, scene load, etc. in the Inspector.
    public UnityEvent onDialogueFinished;

    private string[] dialogueLines =
    {
        // SCENE 1: He enters the house
        "NARRATOR: He follows the shadow through the dark streets until it enters an abandoned house.",
        "CHARACTER: Hey! Wait for me!",
        "NARRATOR: He enters the house, and suddenly the door slams shut.",
        "CHARACTER: What?! The door is locked! How am I supposed to get out?",
        "NARRATOR: He tries opening it, but it won't move.",

        // SCENE 2: Finding the key
        "NARRATOR: He notices a small table in the corner. On it lies a note.",
        "[NOTE] The key is hidden where darkness never reaches.",
        "NARRATOR: He looks around and notices a small light coming through a broken window.",
        "NARRATOR: Under the light, he finds a key.",
        "CHARACTER: Finally! Let's get out of here.",
        "NARRATOR: As he picks up the key, he sees the shadow monster standing behind him.",
        "CHARACTER: Oh no...",
        "NARRATOR: The monster disappears, and he runs toward the door.",

        // SCENE 3: The final escape
        "[SCENE 3: THE FINAL ESCAPE]",
        "NARRATOR: He unlocks the door, but the hallway outside has become completely dark.",
        "NARRATOR: The monster appears at the other end and starts running toward him.",
        "NARRATOR: He rushes outside just as the creature reaches the doorway.",
        "CHARACTER: What was that thing?! Why is it following me?",
        "NARRATOR: He runs away from the house. Behind him, the door slowly closes by itself.",
        "NARRATOR: He thinks he has escaped, but he doesn't notice the black shadow following him on the ground.",
        "[CUT TO BLACK]"
    };

    private int currentLine = 0;
    private bool dialogueStarted = false;
    private int startFrame = -1;

    // Called by StoryDialogue when Level 1 ends.
    public void BeginDialogue()
    {
        currentLine = 0;
        dialogueStarted = true;
        startFrame = Time.frameCount; // ignore the key press that started us
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    void Update()
    {
        if (!dialogueStarted || Time.frameCount == startFrame)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            currentLine++;

            if (currentLine < dialogueLines.Length)
            {
                ShowCurrentLine();
            }
            else
            {
                dialoguePanel.SetActive(false);
                dialogueStarted = false;
                onDialogueFinished?.Invoke();
            }
        }
    }

    void ShowCurrentLine()
    {
        dialogueText.text = dialogueLines[currentLine];
    }
}