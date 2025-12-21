using UnityEngine;
using TMPro;

namespace DorkyProductions.UI
{
    
public class MatchTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;

    private float _currentTime;
    private bool _isRunning;
    
    // Track the last second we updated the UI for to prevent redundant updates
    private int _lastSecond = -1;

    private void OnEnable()
    {
        UIMediator.OnGameStarted += BeginTimer;
        UIMediator.OnGameEnded += StopTimer;
    }

    private void OnDisable()
    {
        UIMediator.OnGameStarted -= BeginTimer;
        UIMediator.OnGameEnded -= StopTimer;
    }

    private void BeginTimer()
    {
        _currentTime = 0f;
        _lastSecond = -1; // Force an immediate update
        _isRunning = true;
        
        // Initial update
        UpdateDisplay(0);
    }

    private void StopTimer(PlayerType winner)
    {
        _isRunning = false;
    }

    private void Update()
    {
        if (!_isRunning) return;

        _currentTime += Time.deltaTime;

        // We only care about the floor value for the display (00:00).
        int currentSecond = (int)_currentTime;

        // OPTIMIZATION: Only touch the TextMeshPro component if the second has changed.
        // This reduces UI calls from 60+ per second to exactly 1 per second.
        if (currentSecond != _lastSecond)
        {
            UpdateDisplay(currentSecond);
            _lastSecond = currentSecond;
        }
    }

    private void UpdateDisplay(int totalSeconds)
    {
        float minutes = totalSeconds / 60;
        float seconds = totalSeconds % 60;

        // SetText parses the format internally without creating a standard C# string.
        // This avoids the GC allocation associated with string.Format().
        _timerText.SetText("{0:00}:{1:00}", minutes, seconds);
        // PS: I don't really trust this, but also have no better solution, so lets hope it does what it says :)
    }
}
}