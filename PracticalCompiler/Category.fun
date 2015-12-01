

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

category : type -> type;
category = struct {
  object : type;
  (~>) : relation object;

  identity : [x:object] -> (x ~> x);
  compose : [x:object, y:object, z:object] -> [f:x ~> y, g:y ~> z] -> (x ~> z);
  
  identityLeft : [x:object, y:object] -> (f:x ~> y) -> (compose (x, x, y) (identity x, f)) == f;
  identityRight : [x:object, y:object] -> (f:x ~> y) -> (compose (x, y, y) (f, identity y)) == f;
  associativity : [w:object, x:object, y:object, z:object] -> [f:w ~> x, g:x ~> y, h:y ~> z] -> (compose (w, y, z) (compose (w, x, y) (f, g), h)) == (compose (w, x, z) (f, compose (x, y, z) (g, h)));
};

isomorphism : [a:category] -> relation (a .object);
isomorphism a [x, y] = struct {
  forward : a .(~>) x y;
  backward : a .(~>) y x;

  forwardId : (a .null x) == (a .compose (x, y, x) (forward, backward));
  backwardId : (a .compose (y, x, y) (backward, forward)) == (a .null y);
};

opposite : category -> category;
opposite a = new {
  object = a .object;
  (~>) [x, y] = a .(~>) y x;

  identity = a .identity;
  compose [x, y, z] [f, g] = a .compose (z, y, x) (g, f);

  identityLeft [x, y] = a .identityRight (y, x);
  identityRight [x, y] = a .identityLeft (y, x);
  associativity [w, x, y, z] [f, g, h] = a .associativity (z, y, x, w) (h, g, f);
};

functor : relation category;
functor [a, b] = struct {
  map : a .object -> b .object;
  fmap : [x:a .object, y:a .object] -> a .(~>) (x, y) -> b .(~>) (map x, map y);

  identity : [x:a .object] -> fmap (x, x) (a .null x) == b .null (map x);
  compose : [x:a .object, y:a .object, z:a .object] -> [f:a .(~>) (x, y), g:a .(~>) (y, z)] -> fmap (x, z) (a .compose (x, y, z) (f, g)) == b .compose (map x, map y, map z) (fmap (x, y) f, fmap (y, z) g);
};

natural : [a:category, b:category] -> relation (functor a b)
natural [a, b] [F, G] = struct {
  transform : [x:a .object] -> b .(~>) (F .map x, G .map x);

  commute : [x:a .object, y:a .object] -> [f:a .(~>) (x, y)] -> b .compose (F .map x, F .map y, G .map y) (F .fmap (x, y) f, transform y) == b .compose (F .map x, G .map x, G .map y) (transform x, G .fmap (x, y) f);
};

algebra : [a:category] -> functor (a, a) -> type;
algebra a F = struct {
  carrier : a .object;
  reduce : a .(~>) (F .map carrier, carrier);
};

homomorphism : [a:category] -> [F:functor (a, a)] -> relation (algebra a F);
homomorphism a F [r, s] = struct {
  morph : a .(~>) (r .carrier, s .carrier);
  
  commute : a .compose (F .map (r .carrier), r .carrier, s .carrier) (r .reduce, morph) == a .compose (F .map (r .carrier), F .map (s .carrier), s .carrier) (F .fmap (r .carrier, s .carrier) morph, s .reduce);
};

algebraic : [a:category] -> functor (a, a) -> category;
algebraic a F = new {
  object = algebra a F;
  (~>) = homomorphism a F;

  null r = new {
    morph = a .null (r .carrier);

	commute = reflexive (a .(~>) (F .map (r .carrier), r.carrier)) (r .reduce);
  };

  compose [r, s, t] [f, g] = new {
    morph = a .compose (r .carrier, s .carrier, t .carrier) (f .morph, g .morph);
	
	/*

	compose (fmap map [morph]) >> reduce >> compose [morph]
	
    commute = startEmpty (r, r, t) >> move (r, r, s, t) >> move (r, s, t, t) >> endEmpty (r, t, t)
	*/
  };
  
  identityLeft [x, y] f = a .identityLeft (x .carrier, y .carrier) (f .morph);  
  identityRight [x, y] f = a .identityRight (x .carrier, y .carrier) (f .morph);  
  compose [w, x, y, z] [f, g, h] = a .compose (w .carrier, x .carrier, y .carrier, z .carrier) (f .morph, g .morph, h .morph);
};

initial : category -> type;
initial a = struct {
  bottom : a .object;
  project : [x:a .object] -> a .(~>) (bottom, x);

  factor : [x:a .object, y:a .object] -> [f:a .(~>) (x, y)] -> a .compose (bottom, x, y) (project x, f) == project y;
};

recursion : [a:category] -> [F:functor (a, a)] -> initial (algebraic a F);
recursion a F = new {
  bottom = new {
    carrier = F .map carrier;
	reduce = a .null carrier;
  };

  project r = new {
    morph = a .compose (F .map (bottom .carrier), F .map (r .carrier), r .carrier) (F .fmap (bottom .carrier, r .carrier) morph, r .reduce);

	commute = reflexive (a .(~>) (bottom .carrier, r .carrier)) morph;
  };
};

fix : [a:category] -> functor (a, a) -> a;
fix a F = recursion a F .bottom .carrier;

catamorphism : [a:category] -> (F:functor (a, a)) -> [r:a .object] -> a .(~>) (F .map r, r) -> a .(~>) (fix a F, r);
catamorphism a F r step = recursion a F .project (new { carrier = r; reduce = step; }) .morph;

0