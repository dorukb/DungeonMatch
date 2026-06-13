using System.Collections;
using DorkyProductions;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public GameObject tutorialCanvas;

    private bool isStarted = false;
    // This flag acts as our "traffic light" for the Coroutine
    private static bool isWaitingForPlayerAction = false;
    public void StartTutorial(ClientBoardVisualizer _visualizer)
    {
        if (isStarted) return;
        isStarted = true;
        tutorialCanvas.SetActive(true);
        // Kick off the sequence
        StartCoroutine(TutorialStepsFlow(_visualizer));
    }
    private IEnumerator TutorialStepsFlow(ClientBoardVisualizer _visualizer)
    {
        // --- STEP 1 ---
        // 1. Turn on the red light
        isWaitingForPlayerAction = true;
        // 2. Setup the visual and interaction state
        _visualizer.MakeTileInteractiveWithinTutorial(2);
        // 3. Pause the coroutine here until the light turns green
        yield return new WaitUntil(() => isWaitingForPlayerAction == false);

        // (Optional) Add a tiny pause so the UI doesn't snap jarringly to the next step
        yield return new WaitForSeconds(0.5f);

        // --- STEP 2 ---
        isWaitingForPlayerAction = true;
        _visualizer.MakeTileInteractiveWithinTutorial(5);
        yield return new WaitUntil(() => isWaitingForPlayerAction == false);

        // --- TUTORIAL COMPLETE ---
        tutorialCanvas.SetActive(false);
        Debug.Log("Tutorial Finished!");
        
        FinishTutorial();

    }

    // You will call this method from whatever script handles the actual clicking
    public static void CompleteCurrentStep()
    {
        isWaitingForPlayerAction = false;
    }
    
    private void FinishTutorial()
    {
        PlayerLocalSave.SetTutorialCompleted();
    }
}
