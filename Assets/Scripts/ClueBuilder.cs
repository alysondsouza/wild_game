using System.Collections.Generic;
using UnityEngine;

// Structured clue data — passed to ClueRow for display.
public struct ClueData
{
    public List<int> Guess;
    public int Correct;
    public int Misplaced;
    public int Wrong;
}

// Builds the minimum set of clues needed to uniquely identify the secret.
// Pure C# — no MonoBehaviour.
public class ClueBuilder
{
    private readonly CodeMaker _maker;
    private readonly int _length;
    private readonly int _digitMin;
    private readonly int _digitMax;
    private readonly int _maxAttempts;

    public ClueBuilder(CodeMaker maker, int length, int digitMin, int digitMax, int maxAttempts = 200)
    {
        _maker = maker;
        _length = length;
        _digitMin = digitMin;
        _digitMax = digitMax;
        _maxAttempts = maxAttempts;
    }

    // Returns structured ClueData list instead of plain strings.
    public List<ClueData> Build(out List<int> secret)
    {
        secret = _maker.Secret;

        var candidates = GenerateAllCandidates();
        var clues = new List<ClueData>();
        var usedFeedbacks = new HashSet<(int, int, int)>();

        int attempts = 0;

        while (candidates.Count > 1 && attempts < _maxAttempts)
        {
            attempts++;

            var guess = _maker.RandomGuess();
            var feedback = _maker.Evaluate(guess);

            if (usedFeedbacks.Contains(feedback)) continue;
            if (feedback.correct == _length || feedback.wrong == _length) continue;

            usedFeedbacks.Add(feedback);

            var newCandidates = new List<List<int>>();
            foreach (var c in candidates)
                if (_maker.IsConsistent(c, guess, feedback))
                    newCandidates.Add(c);

            if (newCandidates.Count < candidates.Count)
            {
                candidates = newCandidates;
                clues.Add(new ClueData
                {
                    Guess = new List<int>(guess),
                    Correct = feedback.correct,
                    Misplaced = feedback.misplaced,
                    Wrong = feedback.wrong
                });
            }
        }

        return clues;
    }

    private List<List<int>> GenerateAllCandidates()
    {
        var result = new List<List<int>>();
        GenerateRecursive(new List<int>(), result);
        return result;
    }

    private void GenerateRecursive(List<int> current, List<List<int>> result)
    {
        if (current.Count == _length) { result.Add(new List<int>(current)); return; }
        for (int d = _digitMin; d <= _digitMax; d++)
        {
            current.Add(d);
            GenerateRecursive(current, result);
            current.RemoveAt(current.Count - 1);
        }
    }
}