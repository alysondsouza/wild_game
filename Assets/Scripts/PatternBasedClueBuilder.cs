using System.Collections.Generic;
using UnityEngine;

// Constructs a guess that will produce a specific (correct, misplaced) feedback
// when evaluated against the secret. No full-space scanning — O(codeLength).
//
// Algorithm (mirrors Wild.Core/Strategy/PatternBasedClueBuilder.cs):
//   1. CORRECT slots  — copy secret[i] at position i
//   2. MISPLACED slots — copy secret[i] at position j != i (forces a mismatch)
//   3. WRONG slots    — fill with digits not present in the secret at all
//
// Returns null if the pattern can't be satisfied (e.g. no non-secret digits
// available for wrong slots). Caller should skip that pattern.
public static class PatternBasedClueBuilder
{
    private const int MaxAttempts = 50; // retry if misplaced placement collides

    public static List<int> BuildGuess(List<int> secret, int correct, int misplaced, System.Random rng)
    {
        int length = secret.Count;
        int wrong  = length - correct - misplaced;

        if (correct < 0 || misplaced < 0 || wrong < 0) return null;
        if (correct == length) return null; // that is the answer

        // Digits not in the secret — used for wrong-slot filler
        var secretSet       = new HashSet<int>(secret);
        var nonSecretDigits = new List<int>();
        for (int d = 0; d <= 9; d++)
            if (!secretSet.Contains(d))
                nonSecretDigits.Add(d);

        if (nonSecretDigits.Count == 0 && wrong > 0) return null;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var guess   = new int[length];
            var usedPos = new HashSet<int>();
            bool failed = false;

            // --- Step 1: place CORRECT digits ---
            var allPositions     = new List<int>();
            for (int i = 0; i < length; i++) allPositions.Add(i);
            var correctPositions = PickDistinct(allPositions, correct, rng);

            foreach (int pos in correctPositions)
            {
                guess[pos] = secret[pos];
                usedPos.Add(pos);
            }

            // --- Step 2: place MISPLACED digits ---
            // Source positions: secret positions not used by correct slots
            var remainingSources = new List<int>();
            foreach (int i in allPositions)
                if (!correctPositions.Contains(i))
                    remainingSources.Add(i);

            var misplacedSources = PickDistinct(remainingSources, misplaced, rng);

            // Free positions not yet assigned
            var freePositions = new List<int>();
            foreach (int i in allPositions)
                if (!usedPos.Contains(i))
                    freePositions.Add(i);
            Shuffle(freePositions, rng);

            // Assign each misplaced source to a free position != its own index
            var assignedMisplacedPos = new HashSet<int>();
            foreach (int src in misplacedSources)
            {
                bool placed = false;
                foreach (int freePos in freePositions)
                {
                    if (freePos == src) continue;              // would become correct
                    if (assignedMisplacedPos.Contains(freePos)) continue; // already taken
                    if (usedPos.Contains(freePos)) continue;

                    guess[freePos] = secret[src];
                    usedPos.Add(freePos);
                    assignedMisplacedPos.Add(freePos);
                    placed = true;
                    break;
                }
                if (!placed) { failed = true; break; }
            }

            if (failed) continue;

            // --- Step 3: fill WRONG slots with non-secret digits ---
            int nonSecretIdx = 0;
            Shuffle(nonSecretDigits, rng);

            for (int i = 0; i < length; i++)
            {
                if (usedPos.Contains(i)) continue;

                if (nonSecretIdx >= nonSecretDigits.Count) { failed = true; break; }
                guess[i] = nonSecretDigits[nonSecretIdx++];
                usedPos.Add(i);
            }

            if (failed) continue;

            return new List<int>(guess);
        }

        return null; // could not satisfy pattern after MaxAttempts
    }

    // Picks k distinct items from a list (Fisher-Yates partial shuffle).
    private static List<int> PickDistinct(List<int> source, int k, System.Random rng)
    {
        var copy   = new List<int>(source);
        var result = new List<int>();
        for (int i = 0; i < k && copy.Count > 0; i++)
        {
            int idx = rng.Next(copy.Count);
            result.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
        return result;
    }

    // Fisher-Yates in-place shuffle.
    private static void Shuffle(List<int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j   = rng.Next(i + 1);
            int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}