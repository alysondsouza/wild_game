using System.Collections.Generic;
using UnityEngine;

// Generates the secret code and evaluates guesses against it.
// Pure C# — no MonoBehaviour, no Unity lifecycle methods.
public class CodeMaker
{
    public List<int> Secret { get; private set; }
    private readonly int _length;
    private readonly int _digitMin;
    private readonly int _digitMax;

    public CodeMaker(int length = 4, int digitMin = 1, int digitMax = 6)
    {
        _length = length;
        _digitMin = digitMin;
        _digitMax = digitMax;
        Secret = GenerateCode();
    }

    // Returns a random code of the given length.
    private List<int> GenerateCode()
    {
        var code = new List<int>();
        for (int i = 0; i < _length; i++)
            code.Add(Random.Range(_digitMin, _digitMax + 1)); // inclusive
        return code;
    }

    // Generates a random guess from the full digit range (not filtered).
    public List<int> RandomGuess()
    {
        return GenerateCode();
    }

    // Evaluates a guess against the secret.
    // Returns (correct, misplaced, wrong).
    public (int correct, int misplaced, int wrong) Evaluate(List<int> guess)
    {
        int correct = 0;
        int misplaced = 0;

        var secretLeftovers = new List<int>();
        var guessLeftovers = new List<int>();

        // First pass: find exact matches.
        for (int i = 0; i < _length; i++)
        {
            if (guess[i] == Secret[i])
                correct++;
            else
            {
                secretLeftovers.Add(Secret[i]);
                guessLeftovers.Add(guess[i]);
            }
        }

        // Second pass: find misplaced digits.
        foreach (int g in guessLeftovers)
        {
            if (secretLeftovers.Remove(g)) // Remove returns true if found
                misplaced++;
        }

        int wrong = _length - correct - misplaced;
        return (correct, misplaced, wrong);
    }

    // Checks if a candidate code is consistent with a given clue.
    // Used by ClueBuilder to filter the candidate pool.
    public bool IsConsistent(List<int> candidate, List<int> guess, (int correct, int misplaced, int wrong) feedback)
    {
        // Temporarily swap secret to evaluate candidate against guess.
        var tempMaker = new CodeMakerTemp(candidate, _length);
        var result = tempMaker.Evaluate(guess);
        return result == feedback;
    }
}

// Lightweight helper used only inside IsConsistent — avoids exposing a messy API.
// This is a private utility, not a full class meant for reuse.
internal class CodeMakerTemp
{
    private readonly List<int> _secret;
    private readonly int _length;

    public CodeMakerTemp(List<int> secret, int length)
    {
        _secret = secret;
        _length = length;
    }

    public (int correct, int misplaced, int wrong) Evaluate(List<int> guess)
    {
        int correct = 0;
        var secretLeftovers = new List<int>();
        var guessLeftovers = new List<int>();

        for (int i = 0; i < _length; i++)
        {
            if (guess[i] == _secret[i])
                correct++;
            else
            {
                secretLeftovers.Add(_secret[i]);
                guessLeftovers.Add(guess[i]);
            }
        }

        int misplaced = 0;
        foreach (int g in guessLeftovers)
            if (secretLeftovers.Remove(g))
                misplaced++;

        return (correct, misplaced, _length - correct - misplaced);
    }
}