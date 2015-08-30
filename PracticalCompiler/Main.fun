
/*
TODO:
forall, exists
handle scoping of struct fields correctly
normalize types better
module path in types
enum
simple pair
translucent types
better import

*/

	lib = import "Library.fun";
	
    /*

    boolean : type;
    boolean = union { true; false; }

    bTrue = #true : boolean;

    #some, 3;

    r #some

    option : type -> type;
    option a = union { 
        None;
        Some : a; 
    };

    what : type -> typeof type;
    what a = union {
        left : type;
        right : type -> type;
    };

    left int;
    right list;

    three = Some 3 : option int;

    set a s = struct {
        empty : s;
        singleton : a -> s;
        union : s -> s -> s;
        intersect : s -> s -> s;
        count : s -> nat;
        contains : s -> a -> bool;
    };

    [a <: ordering] -> ([s] & set a s) 
    treeset : struct {
        coll : type -> type;
        selfset a = set a (coll a);
        construct : [a <: ordering] -> set a (coll a)
        map : [a <: selfset] -> [b <: selfset] -> (a -> b) -> coll a -> coll b; 
    }

    
    lambda [fix <: inductive]. fold listF xs 
    lambda [fix:quantifier]. lambda [least:inductive fix]. using least; fold listF xs 
    lambda [fix:quantifier]. lambda [least:inductive fix]. least .fold listF xs



    */

    kind = typeof type;
    
    operation : type -> type;
    operation [a] = a -> a -> a;

    relation : type -> kind;
    relation [a] = a -> a -> type;
    
    (==) : [a] -> relation a;
    (==) a x y = struct {
        cast : [context:a -> type] -> context x -> context y;
    };

    identity : [a <: relation] -> type;
    identity [a] (~) = [x:a] -> (x ~ x);

    inversion : [a <: relation] -> type;
    inversion [a] (~) = [x:a, y:a] -> (x ~ y) -> (y ~ x);

    combination : [a <: relation] -> type;
    combination [a] (~) = [x:a, y:a, z:a] -> (x ~ y) -> (y ~ z) -> (x ~ z);

    equivalence : [a <: relation] -> type;
    equivalence [a] (~) = struct {
        reflexive : identity a (~);
        symmetric : inversion a (~);
        transitive : combination a (~);
    };

    associative : [a <: relation] -> operation a -> type;
    associative a (~) (<>) = [x:a, y:a, z:a] -> (((x <> y) <> z) ~ (x <> (y <> z)));

    // associative : [a] -> [(~) <: combination a] -> type;
    // associative a (~) (<>) = [w:a, x:a, y:a, z:a] -> [p:w ~ x, q:x ~ y, r:y ~ z] -> ((<>) w y z ((<>) w x y p q) r) `== (w ~ z)` ((<>) w x z p ((<>) x y z q r));
    groupoid : [a <: relation] -> type;
    groupoid [a] (~) = struct {
        null : identity a (~);
        inverse : inversion a (~);
        (<>) : combination a (~);

    /*
        inverse_of_null_is_null : [x:a] -> inverse x x (null x) == null x
        inverse_twice_is_noop : [x:a, y:a] -> [p:x ~ y] -> inverse y x (inverse x y p) == p;
        inverse_flips_concat_arguments : [x:a, y:a, z:a] -> [p:x ~ y, q:y ~ z] -> inverse x z ((<>) x y z p q) == (<>) z y x  (inverse y z q) (inverse x y p)
        concat_is_associative : associative a (~) (<>);
    */
    };

    monoid : type -> type;
    monoid m = struct {
        null : m;
        (<>) : m -> m -> m;
    /*
        left_id : [x:m] -> (null <> x) == x;
        right_id : [x:m] -> (x <> null) == x;
        concat_assoc : associative m (<>);
    */
    };

    additive : monoid int;
    additive = new {
        null = 0;
        (<>) = (+);
    };

    multiplicative : monoid int;
    multiplicative = new {
        null = 1;
        (<>) = (*);
    };

    concatenative : monoid string;
    concatenative = new {
        null = "";
        (<>) x y = x ++ y;
    };
    
    collection : type -> type;
    collection a = [m <: monoid] -> (a -> m) -> m;

    bounds : collection int;
    bounds m mon f = mon .(<>) (f 0) (f 1);

    vector : type;
    vector = struct {
        x : int;
        y : int;
    };

    unit : type;
    unit = struct { };

    toplike : type;
    toplike = struct {
        a : type;
        x : a;
    };

    high : toplike;
    high = new { a = int; x = 0; };

    peak : toplike;
    peak = new { a = string; x = ""; };
    
    // Polymorphic types

    quantifier : typeof type;
    quantifier = (type -> type) -> type;
 
    algebra : (type -> type) -> (type -> type);
    algebra f r = f r -> r;

    coalgebra : (type -> type) -> (type -> type);
    coalgebra f r = r -> f r;

    machine : quantifier;
    machine f = [r <: coalgebra f] & r;

    reducer : quantifier;
    reducer f = [r <: algebra f] -> r;

    functor : (type -> type) -> type;
    functor f = struct {
        map : [a, b] -> (a -> b) -> (f a -> f b);
    };

    inductive : quantifier -> type;
    inductive fix = struct {
        fold : [f <: functor] -> fix f -> reducer f;
    };

    coinductive : quantifier -> type;
    coinductive fix = struct {
        unfold : [f <: functor] -> machine f -> fix f;
    };


    top : type;
    top = [a] & a;
    
	bottom : type;
	bottom = [a] -> a;
    
    async : (type -> type) -> (type -> type);
    async f a = [r] -> (a -> f r) -> f r;
    
    (^) : (type -> type) -> int -> (type -> type);
    (^) f n [a] = string;

    (=>) : (type -> type) -> (type -> type) -> type;
    (=>) f g = [a] -> f a -> g a;
    
	monad : (type -> type) -> type;
	monad m = struct {
		lift : [a] -> a -> m a;
		join : [a] -> m (m a) -> m a;
	};

    monadic : (type -> type) -> type;
    monadic m = struct { 
        (>>=) : m => async m;
		flatten : [n:int] -> ((m ^ n) => m);
    };

    pair : type -> type -> type;
    pair a b = struct { 
        unpack : [r] -> (a -> b -> r) -> r;
    };
 
    continuation : type -> type -> type -> type;
    continuation a b r = struct {
        left : a -> r;
        right : b -> r;
    };

    union : type -> type -> type;
    union a b = struct { 
        unpack : [r] -> continuation a b r -> r;
    };
    
    (~) : quantifier;
    (~) = reducer;

    list : type -> type;
    list [a] = [r] ~ union unit (pair a r);

    decidable : type -> type;
    decidable a = union a (a -> bottom);

    conat : type;
    conat = [r] ~ union unit r;

    boolean : type;
    boolean = [a] -> a -> a -> a;
    
    first : boolean;
    first [a] then else = then;

    second : boolean;
    second [a] then else = else;


    /*
    

    (2, 3) : (int & int : type)

    ([a=int], 2:a) : [a] & a

    (2:int, 4:int -> bool)

    int & bool -> string, foo : bar & list -> tree
    
	identity : [a] -> a -> a;
	identity [a] [x:a] = x;

	null = new toplike {
		a = typeof identity;
		x = identity;
	};
*/

    (<,>) : type -> type -> type;
    (<,>) a b = struct {
        first : a;
        second : b;
    };
    
    //equality : type -> type;
    equality [a] = struct {
        (=?) : (a <,> a) -> bool;
    };
    
    origin = new vector {   
        x = lib .two;
        y = lib .four;
    };
    
    (|>) : int -> (int -> int) -> int;
    (|>) x f = f x;

    square : int -> int;
    square x = (*) x x;

    length : vector -> int;
    length v : int = square (v .x) + square (v .y);

    //hello : string;
    hello = "Hello, World!";

    //high .x
    length origin |> lib .return
	
	//hello
	