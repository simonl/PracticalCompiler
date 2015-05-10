
	return : int -> int;
	return x = x;

	two = 1 + 1;

	four = 2 * 2;

	new {
		return = return;

		two : int;
		two = two;

		four : int;
		four = four;
	}

/*
module {
	foo = int -> int;

	identity : int -> int;
	identity = lambda x. x;

	two = plus one one;

	four = times two two;

	three = plus two one;
}

	lib = import "Library.fun";

	times (identity four) (identity (plus three two))
*/