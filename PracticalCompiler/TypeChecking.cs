using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticalCompiler
{
    public static class TypeChecking
    { 
        public static Term.Universe DefaultGenericType
        {
            get { return new Term.Universe(new Universes(0)); }
        }

        public static Classification<TypedTerm> EnsureType(
            Func<string, Classification<dynamic>> doImport,
            Environment<Classification<TypedTerm>> environment, 
            Option<Classification<TypedTerm>> expected, 
            Term term)
        {
            foreach (var type in expected.Each())
            {
                var typed = CheckType(doImport, environment, type, term);

                return type.ShiftDown(typed);
            }

            return InferType(doImport, environment, term);
        }

        public static Classification<TypedTerm> InferType(Func<string, Classification<dynamic>> doImport, Environment<Classification<TypedTerm>> environment, Term term)
        {
            switch (term.Tag)
            {
                case Productions.Universe:
                {
                    var universe = (Term.Universe) term;

                    return new Classification<TypedTerm>(
                        universe: new Universes(universe.Content.Rank + 2),
                        type: new TypedTerm.Universe(new Universes(universe.Content.Rank + 1)),
                        term: new TypedTerm.Universe(universe.Content));
                }
                case Productions.Quantified:
                {
                    var type = (Term.Quantified) term;
                        
                    return EnsureQuantifiedType(doImport, environment, new Option<Classification<TypedTerm>>.None(), type);
                }
                case Productions.Lambda:
                {
                    var constructor = (Term.Lambda)term;

                    var identifier = constructor.Content.Parameter.Identifier;

                    if (environment.Maps(identifier))
                    {
                        throw new ArgumentException("Shadowing identifier: " + identifier);
                    }

                    foreach (var type in constructor.Content.Parameter.Constraint.Each())
                    {
                        var annotation = InferType(doImport, environment, type).Normalized(environment);

                        var binding = annotation.ShiftDown(identifier);

                        environment = environment.Push(binding);

                        var body = InferType(doImport, environment, constructor.Content.Body);

                        if (FreeVariable(body.Type, identifier))
                        {
                            throw new ArgumentException("Return type should not depend on parameter.");
                        }

                        return new Classification<TypedTerm>(
                            universe: body.Universe,
                            type: new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(Polarity.Forall, annotation.Declared("*"), body.Type))),
                            term: new TypedTerm.Constructor(new Constructors.Arrow(new TypedLambda(identifier, body.Term))));
                    }

                    throw new ArgumentException("Cannot infer type of unannotated parameter.");
                }
                case Productions.Apply:
                {
                    var destructor = (Term.Apply) term;

                    var @operator = InferType(doImport, environment, destructor.Content.Operator);

                    var type = (TypedTerm.Type) @operator.Type;
                    var arrow = (TypeStruct.Quantified) type.Content;

                    if (arrow.Content.Polarity != Polarity.Forall)
                    {
                        throw new ArgumentException("Function application operator must have negative polarity.");
                    }

                    var operand = CheckType(doImport, environment, arrow.Content.From.TypeOf(), destructor.Content.Argument);

                    return new Classification<TypedTerm>(
                        universe: @operator.Universe,
                        type: arrow.Content.To,
                        term: new TypedTerm.Destructor(@operator, new Destructors.Arrow(new TypedApply(operand))));
                }
                case Productions.Module:
                {
                    var module = (Term.Module) term;

                    var signature = InferSignature(doImport, environment, module.Content);

                    return new Classification<TypedTerm>(
                        universe: new Universes(1),
                        type: new TypedTerm.Universe(new Universes(0)),
                        term: new TypedTerm.Type(new TypeStruct.Module(signature)));
                }
                case Productions.New:
                {
                    var @new = (Term.New) term;

                    var untyped = @new.Content.Members;

                    var members = new Classification<KeyValuePair<string, TypedTerm>>[untyped.Length];

                    foreach (var index in ArrayOperations.CountUp(untyped.Length))
                    {
                        var member = InferLet(doImport, environment, untyped[index]);

                        var quantifier = member.Fmap(_ => _.Key);

                        members[index] = member;

                        //environment = environment.Push(quantifier);
                    }

                    return new Classification<TypedTerm>(
                        universe: new Universes(0),
                        type: new TypedTerm.Type(new TypeStruct.Module(new Signature(members.Fmap(member => member.Fmap(binding => binding.Key))))),
                        term: new TypedTerm.Constructor(new Constructors.Module(new TypedModule(members.Fmap(member => member.Term.Value)))));
                }
                case Productions.Access:
                {
                    var access = (Term.Access) term;

                    return InferMemberAccess(doImport, environment, access.Content);
                }
                case Productions.Variable:
                {
                    var variable = (Term.Variable) term;

                    var type = environment.Lookup(variable.Content).TypeOf();

                    return type.ShiftDown<TypedTerm>(new TypedTerm.Variable(variable.Content));
                }
                case Productions.Annotation:
                {
                    var annotation = (Term.Annotation) term;

                    var expected = InferType(doImport, environment, annotation.Content.Type).Normalized(environment);

                    var body = CheckType(doImport, environment, expected, annotation.Content.Term);

                    return expected.ShiftDown(body);
                }
                case Productions.LetBinding:
                {
                    var letBinding = (Term.LetBinding)term;
                        
                    if (environment.Maps(letBinding.Content.Identifier))
                    {
                        throw new ArgumentException("Shadowing identifier: " + letBinding.Content.Identifier);
                    }

                    var defined = InferLet(doImport, environment, letBinding.Content);

                    var identifier = defined.Term.Key;

                    environment = environment.Push(identifier, defined.Fmap(_ => _.Value));

                    var continuation = InferType(doImport, environment, letBinding.Continuation);

                    return new Classification<TypedTerm>(
                        universe: continuation.Universe,
                        type: continuation.Type,
                        term: new TypedTerm.Destructor(
                            @operator: new Classification<TypedTerm>(
                                universe: continuation.Universe,
                                type: new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(Polarity.Forall, defined.TypeOf().Declared("*"), continuation.Type))),
                                term: new TypedTerm.Constructor(new Constructors.Arrow(new TypedLambda(identifier, continuation.Term)))),
                            content: new Destructors.Arrow(new TypedApply(operand: defined.Term.Value))));
                }
                case Productions.Constant:
                {
                    var constant = (Term.Constant) term;

                    return constant.Content.Fmap(value => (TypedTerm)new TypedTerm.Constant(value));
                }
                case Productions.TypeOf:
                {
                    var typeOf = (Term.TypeOf) term;

                    return InferType(doImport, environment, typeOf.Content).TypeOf();
                }
                case Productions.Import:
                {
                    var import = (Term.Import) term;

                    return doImport(import.Filename).Fmap(value => (TypedTerm)new TypedTerm.Constant(value));
                    }
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        private static Classification<TypedTerm> EnsureQuantifiedType(Func<string, Classification<dynamic>> doImport, Environment<Classification<TypedTerm>> environment, Option<Classification<TypedTerm>> expected, Term.Quantified type)
        {
            foreach (var context in expected.Each())
            {
                var universe = (TypedTerm.Universe) context.Term;
            }
            
            foreach (var identifier in type.Content.Identifier.Each())
            {
                if (environment.Maps(identifier))
                {
                    throw new ArgumentException("Shadowing identifier: " + identifier);
                }
            }

            Term left;
            switch (type.Content.Left.Tag)
            {
                case TypeConstraints.None:

                    left = DefaultGenericType;

                    break;
                case TypeConstraints.Type:
                    var annotation = (TypeConstraint.Type) type.Content.Left;

                    left = annotation.Content;
                            
                    break;
                case TypeConstraints.Class:
                    var @class = (TypeConstraint.Class) type.Content.Left;

                    var @classed = InferType(doImport, environment, @class.Content);

                    var signature = (TypedTerm.Type) @classed.Type;

                    // class ~ (f : [_:domain:Univ v] -> Univ u : Univ (u+1))
                    // this ~ [x:domain:Univ v] -> [_:f x:Univ u] -> to

                    if (signature.Content.Tag == TypeStructs.Quantified)
                    {
                        var quantified = (TypeStruct.Quantified) signature.Content;

                        if (quantified.Content.Polarity == Polarity.Forall)
                        {
                            if (quantified.Content.To.Tag != TypedProductions.Universe)
                            {
                                throw new ArgumentException("Type class must map to a type.");
                            }

                            var classUniverse = (TypedTerm.Universe) quantified.Content.To;

                            var domain = quantified.Content.From.TypeOf();

                            var identifier = type.Content.Identifier.Get();

                            var from = domain.Declared(identifier);
                                        
                            environment = environment.Push(from);
                                        
                            var range = EnsureType(doImport, environment, expected, type.Content.Right);

                            if (range.Type.Tag != TypedProductions.Universe)
                            {
                                throw new ArgumentException("Quantifiers relate types, not terms.");
                            }
                                    
                            return range.Fmap<TypedTerm, TypedTerm>(rangeTerm =>
                                new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(
                                    polarity: type.Content.Polarity,
                                    @from: from,
                                    to: new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(
                                            polarity: type.Content.Polarity, 
                                            @from: new Classification<string>(
                                                universe: classUniverse.Content,
                                                type: new TypedTerm.Destructor(@classed, new Destructors.Arrow(new TypedApply(new TypedTerm.Variable(identifier)))), 
                                                term: "*"), 
                                            to: rangeTerm)))))));
                        }
                    }

                    throw new ArgumentException("Type class must be a function.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            {
                var domain = InferType(doImport, environment, left);

                var from = domain.Declared(type.Content.Identifier.Or("*"));

                environment = environment.Push(from);

                var range = EnsureType(doImport, environment, expected, type.Content.Right);

                if (range.Type.Tag != TypedProductions.Universe)
                {
                    throw new ArgumentException("Quantifiers relate types, not terms.");
                }

                return range.Fmap<TypedTerm, TypedTerm>(rangeTerm =>
                    new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(
                        polarity: type.Content.Polarity,
                        @from: from,
                        to: rangeTerm))));
            }
        }

        private static Classification<TypedTerm> InferMemberAccess(Func<string, Classification<dynamic>> doImport, Environment<Classification<TypedTerm>> environment, MemberAccess memberAccess)
        {
            var @operator = InferType(doImport, environment, memberAccess.Operator);

            var type = (TypedTerm.Type) @operator.Type;
            var module = (TypeStruct.Module) type.Content;

            var quantifiers = module.Content.Members;

            foreach (var index in ArrayOperations.CountUp(quantifiers.Length))
            {
                var @class = quantifiers[index];

                if (@class.Term.Equals(memberAccess.Name))
                {
                    // TODO: Substitute dependent type variables to index into @operator
                    return @class.Fmap<string, TypedTerm>(
                            convert:  _ => new TypedTerm.Destructor(@operator, new Destructors.Module(new TypedMemberAccess((uint) index))));
                    
                }
            }

            throw new ArgumentException("Module does not contain member: " + memberAccess.Name);
        }

        private static Signature InferSignature(Func<string, Classification<dynamic>> doImport, Environment<Classification<TypedTerm>> environment, ModuleType moduleType)
        {
            var untyped = moduleType.Members;

            var members = new Classification<string>[untyped.Length];

            foreach (var index in ArrayOperations.CountUp(untyped.Length))
            {
                var binding = untyped[index];

                var typed = InferType(doImport, environment, binding.Value).Normalized(environment);

                var quantifier = typed.ShiftDown(binding.Key);

                members[index] = quantifier;

                environment = environment.Push(quantifier);
            }

            return new Signature(members);
        }

        private static Classification<KeyValuePair<string, TypedTerm>> InferLet(Func<string, Classification<dynamic>> doImport, Environment<Classification<TypedTerm>> environment, Definition definition)
        {
            return InferType(doImport, environment, definition.Body)
                .Fmap(term => new KeyValuePair<string, TypedTerm>(definition.Identifier, term));
        }

        public static TypedTerm CheckType(Func<string, Classification<dynamic>> doImport, Environment<Classification<TypedTerm>> environment, Classification<TypedTerm> expected, Term term)
        {
            switch (term.Tag)
            {
                case Productions.Universe:
                {
                    var universe = (Term.Universe)term;

                    var higher = (TypedTerm.Universe)expected.Term;

                    if (higher.Content.Rank == universe.Content.Rank + 1)
                    {
                        var @checked = new TypedTerm.Universe(universe.Content);

                        return @checked;
                    }

                    throw new ArgumentException("Universe must be within a higher universe.");
                }
                case Productions.Quantified:
                {
                    var type = (Term.Quantified)term;

                    return EnsureQuantifiedType(doImport, environment, expected.Some(), type).Term;
                }
                case Productions.Lambda:
                {
                    var constructor = (Term.Lambda)term;

                    var type = (TypedTerm.Type)expected.Term;
                    var arrow = (TypeStruct.Quantified)type.Content;

                    if (arrow.Content.Polarity != Polarity.Forall)
                    {
                        throw new ArgumentException("Type constraint on lambda is of the wrong polarity.");
                    }

                    var identifier = constructor.Content.Parameter.Identifier;

                    if (environment.Maps(identifier))
                    {
                        throw new ArgumentException("Shadowing identifier: " + identifier);
                    }

                    foreach (var declared in constructor.Content.Parameter.Constraint.Each())
                    {
                        var annotation = InferType(doImport, environment, declared).Normalized(environment);

                        if (!annotation.IsEqualTo(arrow.Content.From.TypeOf()))
                        {
                            throw new ArgumentException("Unexpected domain in lambda term.");
                        }
                    }

                    var binding = arrow.Content.From.TypeOf().ShiftDown(identifier);

                    environment = environment.Push(binding);

                    var domain = expected.TypeOf().ShiftDown(arrow.Content.To);

                    var body = CheckType(doImport, environment, domain, constructor.Content.Body);

                    return new TypedTerm.Constructor(new Constructors.Arrow(new TypedLambda(identifier, body)));
                }
                case Productions.Apply:
                {
                    var destructor = (Term.Apply)term;

                    var argument = InferType(doImport, environment, destructor.Content.Argument);

                    var arrow = expected.Fmap<TypedTerm, TypedTerm>(@return => new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(Polarity.Forall, argument.TypeOf().Declared("*"), @return))));

                    var @operator = CheckType(doImport, environment, arrow, destructor.Content.Operator);

                    return new TypedTerm.Destructor(arrow.ShiftDown(@operator), new Destructors.Arrow(new TypedApply(argument.Term)));
                }
                case Productions.Module:
                {
                    var module = (Term.Module) term;

                    var universe = (TypedTerm.Universe) expected.Term;

                    var signature = InferSignature(doImport, environment, module.Content);

                    return new TypedTerm.Type(new TypeStruct.Module(signature));
                }
                case Productions.New:
                {
                    var @new = (Term.New) term;

                    var type = (TypedTerm.Type) expected.Term;
                    var module = (TypeStruct.Module) type.Content;

                    var length = @new.Content.Members.Length;
                    if (length != module.Content.Members.Length)
                    {
                        throw new ArgumentException("Mismatch between size of signature and module.");
                    }

                    TypedTerm[] newStruct = new TypedTerm[length];

                    foreach (var index in ArrayOperations.CountUp(length))
                    {
                        var quantifier = module.Content.Members[index];

                        var definition = @new.Content.Members[index];

                        if (quantifier.Term != definition.Identifier)
                        {
                            throw new ArgumentException("Mismatch between signature and module member name: " + quantifier.Term);
                        }

                        var memberType = quantifier.TypeOf().Normalized(environment);

                        var body = newStruct[index] = CheckType(doImport, environment, memberType, definition.Body);

                        var bodySubstitution = memberType.ShiftDown(body);

                        if (quantifier.Universe.Rank != 0)
                        {
                            bodySubstitution = bodySubstitution.Normalized(environment);
                        }

                        environment = environment.Push(quantifier.Term, bodySubstitution.Normalized(environment));
                    }

                    return new TypedTerm.Constructor(new Constructors.Module(new TypedModule(newStruct)));
                }
                case Productions.Access:
                {
                    var access = (Term.Access) term;

                    var typed = InferMemberAccess(doImport, environment, access.Content);

                    if (!typed.TypeOf().IsEqualTo(expected))
                    {
                        throw new ArgumentException("Mismatch between member type and type constraints: " + access.Content.Name);
                    }

                    return typed.Term;
                }
                case Productions.Variable:
                {
                    var variable = (Term.Variable)term;

                    var type = environment.Lookup(variable.Content).TypeOf();

                    if (!type.IsEqualTo(expected))
                    {
                        throw new ArgumentException("Mismatch between type declaration and type constraints: " + variable.Content);
                    }

                    return new TypedTerm.Variable(variable.Content);
                }
                case Productions.Annotation:
                {
                    var annotation = (Term.Annotation)term;

                    var type = InferType(doImport, environment, annotation.Content.Type).Normalized(environment);

                    if (!type.IsEqualTo(expected))
                    {
                        throw new ArgumentException("Mismatch between type annotation and type constraints.");
                    }

                    return CheckType(doImport, environment, expected, annotation.Content.Term);
                }
                case Productions.LetBinding:
                {
                    var letBinding = (Term.LetBinding) term;
                        
                    if (environment.Maps(letBinding.Content.Identifier))
                    {
                        throw new ArgumentException("Shadowing identifier: " + letBinding.Content.Identifier);
                    }

                    var defined = InferLet(doImport, environment, letBinding.Content);

                    var identifier = defined.Term.Key;

                    environment = environment.Push(identifier, defined.Fmap(_ => _.Value));

                    var continuation = CheckType(doImport, environment, expected, letBinding.Continuation);

                    return new TypedTerm.Destructor(
                        @operator: new Classification<TypedTerm>(
                            universe: expected.Declared("*").Universe,
                            type: new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(Polarity.Forall, defined.TypeOf().Declared("*"), expected.Term))),
                            term: new TypedTerm.Constructor(new Constructors.Arrow(new TypedLambda(identifier, continuation)))), 
                        content: new Destructors.Arrow(new TypedApply(@operand: defined.Term.Value)));
                }
                case Productions.Constant:
                {
                    var constant = (Term.Constant) term;

                    if (!constant.Content.TypeOf().IsEqualTo(expected))
                    {
                        throw new ArgumentException("Mismatch between constant expression and type constraint.");
                    }

                    return new TypedTerm.Constant(constant.Content.Term);
                }
                case Productions.TypeOf:
                {
                    var typeOf = (Term.TypeOf) term;

                    var content = InferType(doImport, environment, typeOf.Content).TypeOf();

                    if (!content.TypeOf().IsEqualTo(expected))
                    {
                        throw new ArgumentException("Mismatch between type of expression and type constraint.");
                    }

                    return content.Term;
                }
                case Productions.Import:
                {
                    var import = (Term.Import) term;

                    var content = doImport(import.Filename);

                    if (!content.TypeOf().IsEqualTo(expected))
                    {
                        throw new ArgumentException("Mismatch between type of import and type constraint.");
                    }

                    return new TypedTerm.Constant(content.Term);
                }
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        public static Classification<TypedTerm> Normalized(this Classification<TypedTerm> expression, Environment<Classification<TypedTerm>> environment)
        {
            //return expression.TypeOf().ShiftDown(expression.Term);
            return expression.Fmap(term =>
            {
                uint count = 0;
                return Normal(expression, environment.Fmap(_ => _.Term), ref count);
            });
        }

        public static TypedTerm Normal(Classification<TypedTerm> expression, Environment<TypedTerm> environment, ref uint count)
        {
            switch (expression.Term.Tag)
            {
                case TypedProductions.Universe:
                {
                    return expression.Term;
                }
                case TypedProductions.Type:
                {
                    var type = (TypedTerm.Type) expression.Term;

                    switch (type.Content.Tag)
                    {
                        case TypeStructs.Quantified:
                            var arrow = (TypeStruct.Quantified) type.Content;

                            var from = arrow.Content.From.TypeOf();

                            from = new Classification<TypedTerm>(from.Universe, from.Type, Normal(from, environment, ref count));

                            var substitution = PushSubstitution(ref environment, ref count, arrow.Content.From.Term);

                            var to = Normal(expression.Fmap(_ => arrow.Content.To), environment, ref count);

                            return new TypedTerm.Type(new TypeStruct.Quantified(new TypedQuantifier(arrow.Content.Polarity, from.Declared(substitution), to)));
                        case TypeStructs.Module:
                            var module = (TypeStruct.Module) type.Content;

                            var members = new Classification<string>[module.Content.Members.Length];
                            for (uint i = 0; i < members.Length; i++)
                            {
                                var member = module.Content.Members[i];

                                var normalMember = Normal(member.TypeOf(), environment, ref count);

                                members[i] = new Classification<string>(member.Universe, normalMember, member.Term);
                            }

                            return new TypedTerm.Type(new TypeStruct.Module(new Signature(members)));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Constructor:
                {
                    var constructor = (TypedTerm.Constructor) expression.Term;

                    var type = (TypedTerm.Type) expression.Type;

                    switch (type.Content.Tag)
                    {
                        case TypeStructs.Quantified:
                            var arrow = (TypeStruct.Quantified) type.Content;
                            var lambda = (Constructors.Arrow) constructor.Content;

                            var substitution = PushSubstitution(ref environment, ref count, lambda.Content.Identifier);

                            var body = Normal(new Classification<TypedTerm>(expression.Universe, arrow.Content.To, lambda.Content.Body), environment, ref count);

                            return new TypedTerm.Constructor(new Constructors.Arrow(new TypedLambda(substitution, body)));
                        case TypeStructs.Module:
                            var module = (TypeStruct.Module) type.Content;
                            var pack = (Constructors.Module) constructor.Content;

                            return expression.Term; // TODO
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Destructor:
                {
                    var destructor = (TypedTerm.Destructor) expression.Term;

                    var @operator = Normal(destructor.Operator, environment, ref count);

                    var type = (TypedTerm.Type) destructor.Operator.Type;

                    switch (type.Content.Tag)
                    {
                        case TypeStructs.Quantified:
                            var arrow = (TypeStruct.Quantified)type.Content;
                            var apply = (Destructors.Arrow)destructor.Content;

                            var operandClass = arrow.Content.From.Fmap(_ => apply.Content.Operand);

                            var operand = Normal(operandClass, environment, ref count);

                            if (@operator.Tag == TypedProductions.Constructor)
                            {
                                var constructor = (TypedTerm.Constructor) @operator;
                                var lambda = (Constructors.Arrow) constructor.Content;

                                environment = environment.Push(lambda.Content.Identifier, operand);

                                return Normal(expression.Fmap(_ => lambda.Content.Body), environment, ref count);
                            }

                            return new TypedTerm.Destructor(destructor.Operator.Fmap(_ => @operator), new Destructors.Arrow(new TypedApply(operand)));
                        case TypeStructs.Module:
                            var module = (TypeStruct.Module)type.Content;
                            var access = (Destructors.Module)destructor.Content;

                            return new TypedTerm.Destructor(destructor.Operator.Fmap(_ => @operator), new Destructors.Module(new TypedMemberAccess(access.Content.Member)));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Variable:
                {
                    var variable = (TypedTerm.Variable) expression.Term;

                    return environment.Lookup(variable.Identifier) ?? expression.Term;
                }
                case TypedProductions.Constant:
                {
                    return expression.Term;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string PushSubstitution(ref Environment<TypedTerm> environment, ref uint count, string identifier)
        {
            var substitution = identifier == "*" ? identifier : (environment.Maps(identifier) ? ("_" + count++) : identifier);

            environment = environment.Push(identifier, new TypedTerm.Variable(substitution));

            return substitution;
        }

        public static bool FreeVariable(TypedTerm term, string identifier)
        {
            switch (term.Tag)
            {
                case TypedProductions.Universe:
                {
                    return false;
                }
                case TypedProductions.Type:
                {
                    var type = (TypedTerm.Type) term;

                    switch (type.Content.Tag)
                    {
                        case TypeStructs.Quantified:
                            var arrow = (TypeStruct.Quantified) type.Content;

                            return FreeVariable(arrow.Content.From.Type, identifier) || FreeVariable(arrow.Content.To, identifier);
                        case TypeStructs.Module:
                            var module = (TypeStruct.Module) type.Content;

                            foreach (var entry in module.Content.Members)
                            {
                                if (FreeVariable(entry.Type, identifier))
                                {
                                    return true;
                                }

                                if (entry.Term == identifier)
                                {
                                    break;
                                }
                            }

                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Constructor:
                {
                    var constructor = (TypedTerm.Constructor) term;

                    switch (constructor.Content.Type)
                    {
                        case TypeStructs.Quantified:
                            var lambda = (Constructors.Arrow) constructor.Content;

                            if (lambda.Content.Identifier == identifier)
                            {
                                return false;
                            }

                            return FreeVariable(lambda.Content.Body, identifier);
                        case TypeStructs.Module:
                            var module = (Constructors.Module) constructor.Content;

                            return module.Content.Members.Any(member => FreeVariable(member, identifier));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Destructor:
                {
                    var destructor = (TypedTerm.Destructor) term;

                    if (FreeVariable(destructor.Operator.Term, identifier))
                    {
                        return true;
                    }

                    switch (destructor.Content.Type)
                    {
                        case TypeStructs.Quantified:
                            var apply = (Destructors.Arrow)destructor.Content;

                            return FreeVariable(apply.Content.Operand, identifier);
                        case TypeStructs.Module:
                            var access = (Destructors.Module)destructor.Content;

                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Variable:
                {
                    var variable = (TypedTerm.Variable)term;

                    return variable.Identifier == identifier;
                }
                case TypedProductions.Constant:
                {
                    return false;
                }
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        public static Classification<B> Fmap<A, B>(this Classification<A> expression, Func<A, B> convert)
        {
            return new Classification<B>(
                universe: expression.Universe,
                type: expression.Type,
                term: convert(expression.Term));
        }

        public static Classification<string> Declared(this Classification<TypedTerm> expression, string identifier)
        {
            return expression.ShiftDown(identifier);
        } 

        public static Classification<TypedTerm> TypeOf<T>(this Classification<T> expression)
        {
            return new Classification<TypedTerm>(
                universe: new Universes(expression.Universe.Rank + 1),
                type: new TypedTerm.Universe(expression.Universe),
                term: expression.Type);
        }

        public static Classification<T> ShiftDown<T>(this Classification<TypedTerm> expression, T term)
        {
            if (expression.Type.Tag != TypedProductions.Universe)
            {
                throw new ArgumentException("Expected a type: " + expression.Term);
            }

            var universe = (TypedTerm.Universe) expression.Type;

            return new Classification<T>(
                universe: universe.Content,
                type: expression.Term,
                term: term);
        } 
    }
}