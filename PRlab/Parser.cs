namespace Lab1;

/// <summary>
/// Parses text made of lines with one integer each, directly from bytes, and counts the signs.
/// The state survives chunk borders, so a number may be split between two reads.
/// Every thread owns its own State: nothing is shared (Lecture 2, step 1: "Don't share").
/// </summary>
public static class Parser
{
    public struct State
    {
        public long Neg, Pos, Zero;
        public int Value;
        public bool Negative, InNumber;

        public readonly Counts ToCounts() => new(Neg, Pos, Zero);
    }

    public static void Feed(byte[] buf, int offset, int count, ref State s)
    {
        // local copies: the loop works on registers, not on the struct in memory
        long neg = s.Neg, pos = s.Pos, zero = s.Zero;
        int value = s.Value;
        bool negative = s.Negative, inNumber = s.InNumber;

        int end = offset + count;
        for (int i = offset; i < end; i++)
        {
            byte b = buf[i];
            if ((uint)(b - '0') <= 9)
            {
                value = value * 10 + (b - '0');
                inNumber = true;
            }
            else if (b == '-')
            {
                negative = true;
                inNumber = true;
            }
            else if (b == '\n')
            {
                if (inNumber)
                {
                    if (value == 0) zero++;
                    else if (negative) neg++;
                    else pos++;
                }
                value = 0; negative = false; inNumber = false;
            }
            // '\r' and anything else is ignored
        }

        s.Neg = neg; s.Pos = pos; s.Zero = zero;
        s.Value = value; s.Negative = negative; s.InNumber = inNumber;
    }

    /// <summary>Ends the last line when the text has no '\n' at the very end.</summary>
    public static void Finish(ref State s)
    {
        if (s.InNumber)
        {
            if (s.Value == 0) s.Zero++;
            else if (s.Negative) s.Neg++;
            else s.Pos++;
        }
        s.Value = 0; s.Negative = false; s.InNumber = false;
    }
}
