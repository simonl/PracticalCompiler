
/*
TODO:
forall,
handle scoping of struct fields correctly
normalize types/at least substitute typedefs
module path in types
enum
simple pair
translucent types
better import
type class quantifiers

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
        join = plus;
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
    
    algebra : (type -> type) -> (type -> type);
    algebra = lambda f. lambda r. f r -> r;

    coalgebra : (type -> type) -> (type -> type);
    coalgebra = lambda f. lambda r. r -> f r;

    // Polymorphic types

    top : type;
    top = [a] & a;
    
	bottom : type;
	bottom = [a] -> a;
 
    functor : (type -> type) -> type;
    functor = lambda f. struct {
        map : [a] -> [b] -> (a -> b) -> (f a -> f b);
    };

    machine : (type -> type) -> type;
    machine = lambda f. [r] & coalgebra f r & r;

    reducer : (type -> type) -> type;
    reducer = lambda f. [r] -> algebra f r -> r;
    
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

    /*
    
    (2, 3) : (int & int : type)

    ([a=int], 2:a) : [a] & a

    (2:int, 4:int -> bool)

    int & bool -> string, foo : bar & list -> tree
    
    machine : (type -> type) -> type;
    machine f = [a <: coalgebra f] & a;

    reducer : (type -> type) -> type;
    reducer f = [a <: algebra f] -> a;
    
    fold : [f <: functor] -> fix f -> reducer f
    unfold : [f <: functor] -> machine f -> cofix f 


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
    square = lambda x. times x x;

    length : vector -> int;
    length = lambda v. plus (square (v .x)) (square (v .y));

    operation : type;
    operation = int -> int -> int;

    identity : int -> int;
    identity = lambda x. x;

    //hello : string;
    hello = "Hello, World!";

    //high .x
    lib .return (length origin)
	//times (identity four) (identity (plus four one))
	//hello
	
	/*
	list : type -> type; 
	list = lambda (a:type). fix (lambda (r:type). Empty | Node (a, r));
	*/
	