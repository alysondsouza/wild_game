using System;

// Defines which feedback patterns are acceptable as puzzle clues.
//
// Philosophy (mirrors Wild.Core):
//   ACCEPT: patterns that give partial info without giving too much away
//   REJECT: all-correct (is the answer), partial all-wrong (useless),
//           too many correct positions, too many misplaced digits,
//           all digits accounted for (correct + misplaced >= length)
public class ClueQualityConfig
{
    public int MaxCorrect   { get; }
    public int MaxMisplaced { get; }

    public ClueQualityConfig(int maxCorrect, int maxMisplaced)
    {
        MaxCorrect   = maxCorrect;
        MaxMisplaced = maxMisplaced;
    }

    // Returns true if this feedback pattern is acceptable as a clue.
    public bool IsAcceptable(int correct, int misplaced, int wrong, int codeLength)
    {
        // Never give away the answer
        if (correct == codeLength) return false;

        // All-wrong is only useful when it covers the full code (eliminates all digits)
        // e.g. 0-0-4 on a 4-digit code: good. 0-0-2 on a 4-digit code: useless.
        if (correct == 0 && misplaced == 0)
            return wrong == codeLength;

        // All digits accounted for = too revealing
        // e.g. 1-3-0 or 0-4-0 tells the player exactly which digits are in the code
        if (correct + misplaced >= codeLength) return false;

        // Too many exact positions revealed
        if (correct > MaxCorrect) return false;

        // Too many misplaced digits revealed
        if (misplaced > MaxMisplaced) return false;

        return true;
    }

    // Returns the config for a given code length.
    // MaxCorrect=1 for short codes, 2 for longer. MaxMisplaced=2 always.
    public static ClueQualityConfig ForLength(int codeLength)
    {
        switch (codeLength)
        {
            case 3: return new ClueQualityConfig(maxCorrect: 1, maxMisplaced: 2);
            case 4: return new ClueQualityConfig(maxCorrect: 1, maxMisplaced: 2);
            case 5: return new ClueQualityConfig(maxCorrect: 2, maxMisplaced: 2);
            case 6: return new ClueQualityConfig(maxCorrect: 2, maxMisplaced: 2);
            case 7: return new ClueQualityConfig(maxCorrect: 2, maxMisplaced: 2);
            default: throw new ArgumentOutOfRangeException("codeLength", "Code length must be between 3 and 7.");
        }
    }
}