
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

    */

    kind = typeof type;

    monoid : type -> type;
    monoid m = struct {
        null : m;
        (<>) : m -> m -> m;
    };

    additive : monoid int;
    additive = new {
        null = 0;
        (<>) = (+);
    };

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
        map : [a] -> [b] -> (a -> b) -> (f a -> f b);
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
    
    identity : type;
    identity = [a] -> a -> a;
    
    async : (type -> type) -> (type -> type);
    async f a = [r] -> (a -> f r) -> f r;

	monad : (type -> type) -> type;
	monad m = struct {
		lift : [a] -> a -> m a;
		join : [a] -> m (m a) -> m a;
	};

    monadic : (type -> type) -> type;
    monadic m = struct { 
        (>>=) : [a] -> m a -> async m a;
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

    /*
    

    (2, 3) : (int & int : type)

    ([a=int], 2:a) : [a] & a

    (2:int, 4:int -> bool)

    int & bool -> string, foo : bar & list -> tree
    
    (==) : [a] -> a -> a -> type;
    (==) a x y = struct {
        cast : [c:a -> type] -> c x -> c y;
    };
    
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

    operation : type;
    operation = int -> int -> int;

    identity : int -> int;
    identity x = x;

    //hello : string;
    hello = "Hello, World!";

    //high .x
    length origin |> lib .return
	
    //identity four * identity (four + one)
	//hello
	