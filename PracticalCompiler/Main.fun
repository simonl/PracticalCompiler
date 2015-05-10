
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
	

    monoid : type -> type;
    monoid = lambda m. struct {
        null : m;
        join : m -> m -> m;
    };

    additive : monoid int;
    additive = new {
        null = 0;
        join = (+);
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
    algebra = lambda f. lambda r. f r -> r;

    coalgebra : (type -> type) -> (type -> type);
    coalgebra = lambda f. lambda r. r -> f r;

    machine : quantifier;
    machine = lambda f. [r <: coalgebra f] & r;

    reducer : quantifier;
    reducer = lambda f. [r <: algebra f] -> r;

    functor : (type -> type) -> type;
    functor = lambda f. struct {
        map : [a] -> [b] -> (a -> b) -> (f a -> f b);
    };

    inductive : quantifier -> type;
    inductive = lambda fix. struct {
        fold : [f <: functor] -> fix f -> reducer f;
    };

    coinductive : quantifier -> type;
    coinductive = lambda fix. struct {
        unfold : [f <: functor] -> machine f -> fix f;
    };



    top : type;
    top = [a] & a;
    
	bottom : type;
	bottom = [a] -> a;
    
    identity : type;
    identity = [a] -> a -> a;
    
    async : (type -> type) -> (type -> type);
    async = lambda f. lambda a. [r] -> (a -> f r) -> f r;

	monad : (type -> type) -> type;
	monad = lambda m. struct {
		lift : [a] -> a -> m a;
		join : [a] -> m (m a) -> m a;
	};

    monadic : (type -> type) -> type;
    monadic = lambda m. struct { 
        continue : [a] -> m a -> async m a;
    };

    pair : type -> type -> type;
    pair = lambda a. lambda b. struct { 
        unpack : [r] -> (a -> b -> r) -> r;
    };
 
    continuation : type -> type -> type -> type;
    continuation = lambda a. lambda b. lambda r. struct {
        left : a -> r;
        right : b -> r;
    };

    union : type -> type -> type;
    union = lambda a. lambda b. struct { 
        unpack : [r] -> continuation a b r -> r;
    };

    list : type -> type;
    list = lambda [a]. reducer (lambda [r]. union unit (pair a r));

    decidable : type -> type;
    decidable = lambda a. union a (a -> bottom);

    /*
    conat : type;
    conat = [r] ~ union unit r;
    */

    /*
    

    (2, 3) : (int & int : type)

    ([a=int], 2:a) : [a] & a

    (2:int, 4:int -> bool)

    int & bool -> string, foo : bar & list -> tree
    
    (==) : [a] -> a -> a -> type;
    (==) = lambda a. lambda x. lambda y. struct {
        cast : [c:a -> type] -> c x -> c y;
    };
    
    equality : type -> type;
    equality = lambda a. struct {
        (=?) : [x:a] -> [y:a] -> decidable ((==) a x y);
    };

	identity : [a] -> a -> a;
	identity = lambda a. lambda x. x;

	null = new toplike {
		a = typeof identity;
		x = identity;
	};
*/

    pair : type -> type -> type;
    pair = lambda a. lambda b. struct {
        first : a;
        second : b;
    };

    equatable : type -> type;
    equatable = lambda a. struct {
        AreEqual : pair a a -> int;
    };

    origin = new vector {   
        x = lib .two;
        y = lib .four;
    };

    square : int -> int;
    square = lambda x. (*) x x;

    length : vector -> int;
    length = lambda v. square (v .x) + square (v .y);

    operation : type;
    operation = int -> int -> int;

    identity : int -> int;
    identity = lambda x. x;

    //hello : string;
    hello = "Hello, World!";

    //high .x
    lib .return (length origin)
	//identity four * identity (four + one)
	//hello
	