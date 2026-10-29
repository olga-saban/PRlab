namespace Lab1;

/// <summary>Result of counting: how many negative, positive and zero numbers.</summary>
public readonly record struct Counts(long Neg, long Pos, long Zero)
{
    public long Total => Neg + Pos + Zero;

    /// <summary>Adds up the per-thread slots. Safe only after Join (Lecture 2, step 1).</summary>
    public static Counts Sum(Counts[] slots)
    {
        long n = 0, p = 0, z = 0;
        foreach (var c in slots) { n += c.Neg; p += c.Pos; z += c.Zero; }
        return new Counts(n, p, z);
    }
}
