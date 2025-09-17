

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace DreamPotato.Core;

/// <summary>
/// The information we know about an instruction originating solely from the code, and not any cpu state.
/// </summary>
record struct Instruction(ushort Offset, Operation Operation, ushort Arg0 = default, ushort Arg1 = default, ushort Arg2 = default) : ISpanFormattable
{
    public bool HasValue => Operation is not null;

    public OperationKind Kind => Operation.Kind;
    public ImmutableArray<Parameter> Parameters => Operation.Parameters;
    public byte Size => Operation.Size;
    public byte Cycles => Operation.Cycles;

    private ushort GetArgument(int i)
    {
        Debug.Assert(i < Parameters.Length);
        return i switch
        {
            0 => Arg0,
            1 => Arg1,
            2 => Arg2,
            _ => Throw()
        };
        static ushort Throw() => throw new ArgumentOutOfRangeException(nameof(i));
    }

    public override string ToString()
    {
        if (!HasValue)
            return "";

        if (Parameters.IsEmpty)
            return Operation.Kind.ToString();

        // TODO: the most interesting/useful display is going to include some(?) cpu state.
        // e.g. if bank 0, can show built-in symbols for that bank. Same for bank 1.
        // Including cpu state really reflects a moment in time interpretation of the instruction though.
        // Same instruction could be run with different cpu states and mean different things.
        // Both forms of display are possibly useful.
        // We can include not only symbols, but, we can also include the values which are being modified, as well as whether branches are taken.
        // In other words logging the execution of the program in quite useful detail.
        var builder = new StringBuilder();
        builder.Append($"{Operation.Kind} ");

        var parameters = Parameters;
        for (var i = 0; i < parameters.Length; i++)
        {
            DisplayArgument(builder, parameters[i], GetArgument(i));

            if (i != parameters.Length - 1)
                builder.Append(", ");
        }
        return builder.ToString();
    }

    private void DisplayArgument(StringBuilder builder, Parameter param, ushort arg)
    {
        if (param.Kind == ParameterKind.D9 && (arg & 0x100) != 0)
        {
            var registerName = SpecialFunctionRegisterIds.GetName((byte)arg);
            if (registerName is not null)
            {
                builder.Append(registerName);
                return;
            }
        }

        var prefix = param.Kind switch
        {
            ParameterKind.I8 => "#",
            ParameterKind.Ri => "@R",
            _ => ""
        };
        builder.Append(prefix);

        var displayValue = param.Kind switch
        {
            ParameterKind.R8 => Offset + Operation.Size + (sbyte)arg,
            ParameterKind.R16 => Offset + arg,
            ParameterKind.A8 => (Offset & 0xff00) | arg,
            ParameterKind.A12 => (Offset & 0xf000) | arg,
            _ => arg
        };
        builder.Append($"{displayValue:X}");
        if (displayValue > 9)
            builder.Append("H");
    }

    private bool DisplayArgument(Parameter param, ushort arg, Span<char> destination, ref int charsWrittenSoFar)
    {
        if (param.Kind == ParameterKind.D9 && (arg & 0x100) != 0)
        {
            var registerName = SpecialFunctionRegisterIds.GetName((byte)arg);
            if (registerName is not null)
            {
                if (!registerName.TryCopyTo(destination[charsWrittenSoFar..]))
                {
                    charsWrittenSoFar = 0;
                    return false;
                }

                charsWrittenSoFar += registerName.Length;
            }
        }

        var prefix = param.Kind switch
        {
            ParameterKind.I8 => "#",
            ParameterKind.Ri => "@R",
            _ => ""
        };

        if (!prefix.TryCopyTo(destination[charsWrittenSoFar..]))
        {
            charsWrittenSoFar = 0;
            return false;
        }

        charsWrittenSoFar += prefix.Length;

        var displayValue = param.Kind switch
        {
            ParameterKind.R8 => Offset + Operation.Size + (sbyte)arg,
            ParameterKind.R16 => Offset + arg,
            ParameterKind.A8 => (Offset & 0xff00) | arg,
            ParameterKind.A12 => (Offset & 0xf000) | arg,
            _ => arg
        };

        if (!destination[charsWrittenSoFar..].TryWrite($"{displayValue:X}", out var charsWritten1))
        {
            charsWrittenSoFar = 0;
            return false;
        }

        charsWrittenSoFar += charsWritten1;

        if (displayValue > 9)
        {
            if (!"H".TryCopyTo(destination[charsWrittenSoFar..]))
            {
                charsWrittenSoFar = 0;
                return false;
            }

            charsWrittenSoFar += "H".Length;
        }

        return true;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!HasValue)
        {
            charsWritten = 0;
            return true;
        }

        if (Parameters.IsEmpty)
        {
            return Enum.TryFormat(Operation.Kind, destination, out charsWritten, format);
        }

        if (!destination.TryWrite($"{Operation.Kind} ", out charsWritten))
        {
            return false;
        }

        var parameters = Parameters;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!DisplayArgument(parameters[i], GetArgument(i), destination, ref charsWritten))
            {
                charsWritten = 0;
                return false;
            }

            if (i != parameters.Length - 1)
            {
                if (!destination[charsWritten..].TryWrite($", ", out var charsWritten1))
                {
                    charsWritten = 0;
                    return false;
                }

                charsWritten += charsWritten1;
            }
        }

        return true;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        throw new NotImplementedException();
    }
}
