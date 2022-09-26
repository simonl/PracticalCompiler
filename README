# PracticalCompiler
Typechecking of a functional language with some useful types.

  Well-Typed expressions are separated into three categories: Universe, Type, Term.
Universes are to types as types are to terms. The base universe is usually called 'type'.
  For example, we have (3 : int), three is an int, and (int : type), 'int' is a type. For short, we can combine these assertions (3 : int : type). This triple structure is mirrored at every level, ("Hello" : string : type), (string : type : kind), (list : type -> type : kind), etc. That is what I mean by the three categories.

  Every expression (e.g. 17) has a corresponding type (int) and universe (type). The universe of an expression is always fully known statically, while the type and term can be abstract (variables, only resolved at runtime).
  For example, (x : string : type) is allowed, even (y : T : type), but not (z : S : U : kind) as U cannot be restricted to be a universe under which a type can be hidden. That is, from the level of a statically known universe, only two levels of abstractions can be made below. e.g. (S : U : kind) but not (z : S : U : kind)

  The level of terms is further divided in two categories: Constructor, Destructor.
  Each type generates a single constructor and destructor. For example, function types generate the constructor 'lambda' and destructor 'function application'. 

Example syntax:

relation : type -> type;  
relation [a] = (a, a) -> type;  
  
(==) : [a] -> relation a;  
(==) a [x, y] = struct {  
  transport : [P:a -> type] -> P x -> P y;  
};  
  
reflexive : [a] -> [x:a] -> (x == x);  
reflexive a x = new {  
  transport P xs = xs;  
};  
