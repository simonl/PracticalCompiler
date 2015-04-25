
/*
TODO:
handle scoping of struct fields correctly
normalize types/at least substitute typedefs
module path in types
enum
simple pair
translucent types
better import

*/

	lib = import "Library.fun";
	
/*
	monad : (type -> type) -> type;
	monad m = struct {
		lift : forall (a:type). a -> m a;
		join : forall (a:type). m (m a) -> m a;
	};
*/

    monoid : type -> type;
    monoid = lambda m. struct {
        null : m;
        join : m -> m -> m;
    };

    additive : monoid int;
    additive = new {
        null = zero;
        join = plus;
    };

    vector : type;
    vector = struct {
        x : int;
        y : int;
    };

    top : type;
    top = struct {
        a : type;
        x : a;
    };

    high : top;
    high = new {
        a = int;
        x = lib .four;
    };

    /*
	bottom : type;
	bottom = forall (a:type). a;

	identity : forall (a:type). a -> a;
	identity = lambda a. lambda x. x;

	null = new top {
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

    high .x
    //lib .return (length origin)
	//times (identity four) (identity (plus four one))
	//hello
	
	/*
	list : type -> type; 
	list = lambda (a:type). fix (lambda (r:type). Empty | Node (a, r));
	*/
	