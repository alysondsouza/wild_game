using System;
using System.Collections.Generic;

// Generates the secret code.
// Pure C# — no MonoBehaviour, no UnityEngine.Random.
// Evaluation logic has moved to ClueBuilder (inline Evaluate method).
public class CodeMaker
{
    public List<int> Secret { get; private set; }

    private readonly int           _length;
    private readonly System.Random _rng;

    public CodeMaker(int length = 4, int digitMin = 0, int digitMax = 9)
    {
        _length = length;
        _rng    = new System.Random();
        Secret  = GenerateCode();
    }

    private List<int> GenerateCode()
    {
        var code = new List<int>();
        for (int i = 0; i < _length; i++)
            code.Add(_rng.Next(0, 10)); // 0–9 inclusive
        return code;
    }
}