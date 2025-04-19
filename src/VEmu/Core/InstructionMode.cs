enum InstructionMode : byte
{
    Immediate = 0b0001, // oooommmm iiiiiiii
    Direct = 0b0010, // oooommmd dddddddd
    Indirect = 0b0100, // oooommjj
}

public static class InstructionModeExtensions
{
    public static InstructionMode GetInstructionMode(byte b)
    {
        var rawMode = (InstructionMode)(b & 0x0f);
        if (rawMode == InstructionMode.Immediate)
            return InstructionMode.Immediate;

        
    }
}