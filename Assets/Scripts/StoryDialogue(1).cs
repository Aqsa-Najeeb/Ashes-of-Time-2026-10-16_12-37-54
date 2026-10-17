/*
using UnityEngine;
using TMPro;

public class StoryDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    private string[] dialogueLines =
    {
        "NARRATOR: The character slowly opens their eyes. Dust floats through the air.",
        "Ugh... what happened?",
        "NARRATOR: He struggles to get up and look around.",
        "Where am I? This isn't where I was...",
        "NARRATOR: He checks their wristwatch.",
        "Wait... what? That's not the right time.",
        "NARRATOR: He notices a clock hanging on the wall. Its time is different.",
        "And that clock... it's different too.",
        "NARRATOR: He looks between the two clocks, confused.",
        "Is my watch broken? No... something isn't right.",
        "NARRATOR: He looks outside at the abandoned city.",
        "What happened to this place?",
        "NARRATOR: After a moment of silence,he take a breath.",
        "I need to find out where I am... and how to get back."
    };

    private int currentLine = 0;
    public StoryDialogue2 level2Dialogue;
    private bool waitingForLevel2 = false;
    void Start()
    {
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    void Update()
    {
        // Wait for E after Level 1 has finished
        if (waitingForLevel2)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                waitingForLevel2 = false;

                if (level2Dialogue != null)
                {
                    level2Dialogue.BeginDialogue();
                }
                else
                {
                    Debug.LogError(
                        "StoryDialogue: Level 2 script is not assigned."
                    );
                }
            }

            return;
        }

        // Normal Level 1 dialogue
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
                waitingForLevel2 = true;
            }
        }
    }

    void ShowCurrentLine()
    {
        dialogueText.text = dialogueLines[currentLine];
    }

    public bool IsFinished()
    {
        return currentLine >= dialogueLines.Length;
    }
}
*/
using UnityEngine;
using TMPro;

public class StoryDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    // Drag the object that has the StoryDialogue2 script here in the Inspector.
    public StoryDialogue2 level2Dialogue;

    private string[] dialogueLines =
    {
        "NARRATOR: The character slowly opens his eyes. Dust floats through the air.",
        "CHARACTER: Ugh... what happened?",
        "NARRATOR: He struggles to get up and look around.",
        "CHARACTER: Where am I? This isn't where I was...",
        "NARRATOR: He checks his wristwatch.",
        "CHARACTER: Wait... what? That's not the right time.",
        "NARRATOR: He notices a clock hanging on the wall. Its time is different.",
        "CHARACTER: And that clock... it's different too.",
        "NARRATOR: He looks between the two clocks, confused.",
        "CHARACTER: Is my watch broken? No... something isn't right.",
        "NARRATOR: He looks outside at the abandoned city.",
        "CHARACTER: What happened to this place?",
        "NARRATOR: After a moment of silence, he takes a breath.",
        "CHARACTER: I need to find out where I am... and how to get back.",
        "NARRATOR: Just then, a dark shadow slips across the street outside.",
        "NARRATOR: Without thinking, he decides to follow it."
    };

    private int currentLine = 0;
    private bool waitingForLevel2 = false;

    void Start()
    {
        // Hide Level 2's panel only if it is a different panel from ours.
        if (level2Dialogue != null &&
            level2Dialogue.dialoguePanel != null &&
            level2Dialogue.dialoguePanel != dialoguePanel)
        {
            level2Dialogue.dialoguePanel.SetActive(false);
        }

        currentLine = 0;
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    void Update()
    {
        bool pressed = Input.GetKeyDown(KeyCode.E) ||
                       Input.GetKeyDown(KeyCode.Space);

        if (!pressed)
            return;

        // Level 1 is finished: next press starts Level 2.
        if (waitingForLevel2)
        {
            waitingForLevel2 = false;

            if (level2Dialogue != null)
            {
                level2Dialogue.BeginDialogue();
            }
            else
            {
                Debug.LogError("StoryDialogue: Level 2 script is not assigned.");
            }

            enabled = false; // Level 1 is done; stop its Update.
            return;
        }

        // Normal Level 1 dialogue.
        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            dialoguePanel.SetActive(false);
            waitingForLevel2 = true;
        }
    }

    void ShowCurrentLine()
    {
        dialogueText.text = dialogueLines[currentLine];
    }
}