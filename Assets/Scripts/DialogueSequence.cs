
using UnityEngine;

public class DialogueSequence : MonoBehaviour
{
    public StoryDialogue level1;
    public StoryDialogue2 level2;

    private bool waitingForLevel2 = false;

    void Update()
    {
        if (level1 == null || level2 == null)
            return;

        // Wait until Level 1 has finished.
        if (!level1.enabled && !level2.enabled &&
            !waitingForLevel2)
        {
            waitingForLevel2 = true;
        }

        // Start Level 2 when E is pressed.
        if (waitingForLevel2 &&
            Input.GetKeyDown(KeyCode.E))
        {
            level2.enabled = true;
            level2.dialoguePanel.SetActive(true);
          //  level2.ShowFirstLine();

            waitingForLevel2 = false;
        }
    }
}
