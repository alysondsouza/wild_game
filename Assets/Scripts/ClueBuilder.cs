using System;
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
//
// Mirrors Wild.Core/Builder/MinClueBuilder.cs + RandomClueStrategy.cs,
// flattened into a single class (no interfaces, no namespaces).
//
// Performance model (same as wild):
//   - Candidates stored as int[] — avoids List<int> object allocation per code.
//   - Candidate list is generated once iteratively, then narrowed each round.
//   - Evaluation of clue quality uses a SAMPLE of 500 when candidates > 2000.
//   - FilterCandidates always runs on the FULL list (not the sample).
//   - After clue 1 the list shrinks dramatically — subsequent rounds are fast.
public class ClueBuilder
{
    private readonly CodeMaker         _maker;
    private readonly int               _length;
    private readonly ClueQualityConfig _config;
    private readonly System.Random     _rng;

    private const int MaxClues        = 20;
    private const int ExactThreshold  = 2000;   // use full list below this for evaluation
    private const int SampleSize      = 500;    // reservoir size above threshold
    private const double AllWrongChance = 0.25;

    private int RelaxThreshold => _length + 2;

    public ClueBuilder(CodeMaker maker, int length, int digitMin, int digitMax)
    {
        _maker  = maker;
        _length = length;
        _config = ClueQualityConfig.ForLength(length);
        _rng    = new System.Random();
    }

    // Builds the minimum clue set. Returns structured ClueData list.
    // 'secret' out-param kept for API compatibility with PuzzleUI.
    public List<ClueData> Build(out List<int> secret)
    {
        secret = _maker.Secret;

        // Generate all 10^length candidates once — iterative (not recursive).
        var candidates   = GenerateAllCandidates();
        var clues        = new List<ClueData>();
        var usedPatterns = new HashSet<string>();
        var usedGuesses  = new HashSet<string>();

        while (candidates.Count > 1)
        {
            if (clues.Count >= MaxClues)
            {
                Debug.LogWarning("[ClueBuilder] Hit MaxClues limit.");
                break;
            }

            var clue = PickClue(candidates, secret, usedPatterns, usedGuesses);

            if (clue == null)
                clue = FallbackClue(candidates, secret, usedPatterns, usedGuesses);

            if (clue == null)
            {
                Debug.LogWarning($"[ClueBuilder] No valid clue with {candidates.Count} candidates left.");
                break;
            }

            usedPatterns.Add(PatternKey(clue.Value.Correct, clue.Value.Misplaced, clue.Value.Wrong));
            usedGuesses.Add(GuessKey(clue.Value.Guess));

            // Filter always runs on the FULL candidate list, not the sample.
            candidates = FilterCandidates(candidates, clue.Value.Guess,
                clue.Value.Correct, clue.Value.Misplaced, clue.Value.Wrong);

            clues.Add(clue.Value);
        }

        return clues;
    }

    // ── Clue selection ────────────────────────────────────────────────────────

    private ClueData? PickClue(
        List<int[]> candidates, List<int> secret,
        HashSet<string> usedPatterns, HashSet<string> usedGuesses)
    {
        var evalSet = candidates.Count <= ExactThreshold
            ? candidates
            : ReservoirSample(candidates, SampleSize);

        var options = BuildOptions(secret, usedPatterns, usedGuesses,
            evalSet, candidates.Count, relaxDiversity: false);

        if (options.Count == 0) return null;
        return SelectByPreference(options);
    }

    private ClueData? FallbackClue(
        List<int[]> candidates, List<int> secret,
        HashSet<string> usedPatterns, HashSet<string> usedGuesses)
    {
        bool relax  = candidates.Count <= RelaxThreshold;
        var evalSet = candidates.Count <= ExactThreshold
            ? candidates
            : ReservoirSample(candidates, SampleSize);

        var options = BuildOptions(secret, usedPatterns, usedGuesses,
            evalSet, candidates.Count, relaxDiversity: relax);

        if (options.Count == 0) return null;

        options.Sort((a, b) => a.EstRem.CompareTo(b.EstRem));
        return options[0].Data;
    }

    // ── Option building ───────────────────────────────────────────────────────

    private struct ClueOption
    {
        public ClueData Data;
        public int      EstRem;
        public int      Correct;
        public int      Misplaced;
    }

    private List<ClueOption> BuildOptions(
        List<int> secret,
        HashSet<string> usedPatterns, HashSet<string> usedGuesses,
        List<int[]> evalSet, int totalCount,
        bool relaxDiversity)
    {
        var options = new List<ClueOption>();

        for (int c = 0; c <= _length; c++)
        for (int m = 0; m <= _length - c; m++)
        {
            int w = _length - c - m;

            if (!_config.IsAcceptable(c, m, w, _length)) continue;
            if (!relaxDiversity && usedPatterns.Contains(PatternKey(c, m, w))) continue;

            var guess = PatternBasedClueBuilder.BuildGuess(secret, c, m, _rng);
            if (guess == null) continue;
            if (usedGuesses.Contains(GuessKey(guess))) continue;

            // Verify actual feedback — BuildGuess targets a pattern but confirm it
            int ac, am, aw;
            Evaluate(guess, secret, out ac, out am, out aw);
            if (!_config.IsAcceptable(ac, am, aw, _length)) continue;
            if (!relaxDiversity && usedPatterns.Contains(PatternKey(ac, am, aw))) continue;

            // Count survivors in the evaluation set (sample or full list)
            int survivors = 0;
            foreach (var cand in evalSet)
            {
                int cc, cm, cw;
                Evaluate(guess, cand, out cc, out cm, out cw);
                if (cc == ac && cm == am && cw == aw) survivors++;
            }

            int estRem = evalSet.Count < totalCount
                ? (int)((double)survivors / evalSet.Count * totalCount)
                : survivors;

            if (estRem < totalCount)
            {
                options.Add(new ClueOption
                {
                    Data      = new ClueData { Guess = guess, Correct = ac, Misplaced = am, Wrong = aw },
                    EstRem    = estRem,
                    Correct   = ac,
                    Misplaced = am
                });
            }
        }

        return options;
    }

    // ── Preference (mirrors RandomClueStrategy.SelectByPreference) ────────────

    private ClueData SelectByPreference(List<ClueOption> options)
    {
        var preferred = new List<ClueOption>();
        var allWrong  = new List<ClueOption>();

        foreach (var opt in options)
        {
            if (opt.Correct == 0 && opt.Misplaced == 1) preferred.Add(opt);
            if (opt.Correct == 0 && opt.Misplaced == 0) allWrong.Add(opt);
        }

        if (preferred.Count > 0)
            return preferred[_rng.Next(preferred.Count)].Data;

        if (allWrong.Count > 0 && _rng.NextDouble() < AllWrongChance)
            return allWrong[_rng.Next(allWrong.Count)].Data;

        options.Sort((a, b) => a.EstRem.CompareTo(b.EstRem));
        return options[0].Data;
    }

    // ── Core helpers ──────────────────────────────────────────────────────────

    // Two-pass Mastermind evaluation against a List<int> target.
    private void Evaluate(List<int> guess, List<int> target,
        out int correct, out int misplaced, out int wrong)
    {
        correct = 0;
        var secretLeft = new List<int>(_length);
        var guessLeft  = new List<int>(_length);

        for (int i = 0; i < _length; i++)
        {
            if (guess[i] == target[i]) correct++;
            else { secretLeft.Add(target[i]); guessLeft.Add(guess[i]); }
        }

        misplaced = 0;
        foreach (int g in guessLeft)
            if (secretLeft.Remove(g)) misplaced++;

        wrong = _length - correct - misplaced;
    }

    // Overload for int[] candidate — avoids List<int> allocation in the hot loop.
    private void Evaluate(List<int> guess, int[] target,
        out int correct, out int misplaced, out int wrong)
    {
        correct = 0;
        var secretLeft = new List<int>(_length);
        var guessLeft  = new List<int>(_length);

        for (int i = 0; i < _length; i++)
        {
            if (guess[i] == target[i]) correct++;
            else { secretLeft.Add(target[i]); guessLeft.Add(guess[i]); }
        }

        misplaced = 0;
        foreach (int g in guessLeft)
            if (secretLeft.Remove(g)) misplaced++;

        wrong = _length - correct - misplaced;
    }

    // Keep only candidates whose feedback matches the clue exactly.
    private List<int[]> FilterCandidates(
        List<int[]> candidates, List<int> guess,
        int correct, int misplaced, int wrong)
    {
        var result = new List<int[]>(candidates.Count / 4);
        foreach (var cand in candidates)
        {
            int c, m, w;
            Evaluate(guess, cand, out c, out m, out w);
            if (c == correct && m == misplaced && w == wrong)
                result.Add(cand);
        }
        return result;
    }

    // Iterative candidate generation — avoids recursion overhead at 10^7.
    private List<int[]> GenerateAllCandidates()
    {
        int total = 1;
        for (int i = 0; i < _length; i++) total *= 10;

        var result = new List<int[]>(total);

        for (int i = 0; i < total; i++)
        {
            var digits = new int[_length];
            int value  = i;
            for (int pos = _length - 1; pos >= 0; pos--)
            {
                digits[pos] = value % 10;
                value /= 10;
            }
            result.Add(digits);
        }

        return result;
    }

    // Reservoir sampling — O(n) time, O(k) space.
    private List<int[]> ReservoirSample(List<int[]> candidates, int k)
    {
        var sample = new List<int[]>(k);
        for (int i = 0; i < candidates.Count; i++)
        {
            if (i < k) sample.Add(candidates[i]);
            else
            {
                int j = _rng.Next(i + 1);
                if (j < k) sample[j] = candidates[i];
            }
        }
        return sample;
    }

    private static string PatternKey(int c, int m, int w) => $"{c}-{m}-{w}";
    private static string GuessKey(List<int> g)           => string.Join(",", g);
}