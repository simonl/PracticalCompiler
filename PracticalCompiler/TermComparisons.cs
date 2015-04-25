using System;

namespace PracticalCompiler
{
    public static class TermComparisons
    {
        public static bool IsEqualTo<T>(this Classification<T> first, Classification<T> second)
        {
            return first.Universe.IsEqualTo(second.Universe)
                   && first.Type.IsEqualTo(second.Type)
                   && first.Term.Equals(second.Term);
        }

        public static bool IsEqualTo(this Universes first, Universes second)
        {
            return first.Rank == second.Rank;
        }

        public static bool IsEqualTo(this TypeStruct first, TypeStruct second)
        {
            if (first.Tag != second.Tag)
            {
                return false;
            }

            switch (first.Tag)
            {
                case TypeStructs.Arrow:
                    var arrow1 = (TypeStruct.Arrow)first;
                    var arrow2 = (TypeStruct.Arrow)second;

                    return arrow1.Content.From.IsEqualTo(arrow2.Content.From)
                           && arrow1.Content.To.IsEqualTo(arrow2.Content.To);
                case TypeStructs.Module:
                    var module1 = (TypeStruct.Module)first;
                    var module2 = (TypeStruct.Module)second;

                    var members1 = module1.Content.Members;
                    var members2 = module2.Content.Members;

                    if (members1.Length != members2.Length)
                    {
                        return false;
                    }

                    foreach (var index in ArrayOperations.CountUp(members1.Length))
                    {
                        if (!members1[index].IsEqualTo(members2[index]))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool IsEqualTo(this Destructors first, Destructors second)
        {
            if (first.Type != second.Type)
            {
                return false;
            }

            switch (first.Type)
            {
                case TypeStructs.Arrow:
                    var apply1 = (Destructors.Arrow)first;
                    var apply2 = (Destructors.Arrow)second;

                    return apply1.Content.Operand.IsEqualTo(apply2.Content.Operand);
                case TypeStructs.Module:
                    var access1 = (Destructors.Module)first;
                    var access2 = (Destructors.Module)second;

                    return access1.Content.Member.Equals(access2.Content.Member);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool IsEqualTo(this TypedTerm first, TypedTerm second)
        {
            if (first.Tag != second.Tag)
            {
                return false;
            }

            switch (first.Tag)
            {
                case TypedProductions.Universe:
                {
                    var universe1 = (TypedTerm.Universe)first;
                    var universe2 = (TypedTerm.Universe)second;

                    return universe1.Content.IsEqualTo(universe2.Content);
                }
                case TypedProductions.Type:
                {
                    var type1 = (TypedTerm.Type)first;
                    var type2 = (TypedTerm.Type)second;

                    return type1.Content.IsEqualTo(type2.Content);
                }
                case TypedProductions.Constructor:
                {
                    return false;
                }
                case TypedProductions.Destructor:
                {
                    var destructor1 = (TypedTerm.Destructor)first;
                    var destructor2 = (TypedTerm.Destructor)second;

                    return destructor1.Operator.IsEqualTo(destructor2.Operator)
                           && destructor1.Content.IsEqualTo(destructor2.Content);
                }
                case TypedProductions.Variable:
                {
                    var variable1 = (TypedTerm.Variable)first;
                    var variable2 = (TypedTerm.Variable)second;

                    return variable1.Identifier == variable2.Identifier;
                }
                case TypedProductions.Constant:
                {
                    return false;
                }
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }
    }
}