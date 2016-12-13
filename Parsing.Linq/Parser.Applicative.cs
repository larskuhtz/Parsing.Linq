using System.Collections.Generic;
using System.Linq;

namespace System.Parsing.Linq
{
    // Not yet used
    public interface Functor<T>
    {
        Functor<T2> FMap<T1, T2>(Func<T1, T2> f, Functor<T1> a); 
    }

    // Not yet used
    public interface Applicative<T> : Functor<T>
    {
        Applicative<T> Pure(T t);

        Applicative<T2> App<T1, T2>(Applicative<Func<T1, T2>> f, Applicative<T1> a);
    }

    // Applicative Functors are a generalization of Monads. They are a strict
    // super class of Monads and, thus, less powerful. As a consequence, 
    // applicative functors have laxer requirements on their execution context. 
    // Often applicative functors can be implemented more efficiently 
    // and provide more opportunities for optimization.
    //
    // The SelectMany parser in Parser.Operators implements a monadic join by
    // evaluating a parser within a parser.
    //
    // >   var res1 = parser.Parse(text, offset);
    // >   if (res1.IsMissing) return ParserResult<TValue2>.Missing;
    // >   var val1 = res1.Value;
    // >   var res2 = selector(val1).Parse(text, offset + res1.Length);
    //
    // The primitive parsers that are defined in this file don't make use of
    // monadic join (which is different from Join in LINQ).
    //
    public static partial class Parser
    {
        /* ****************************************************************** */
        /* Primitive Parsers */

        // Functor
        // Defined in Parser.Operators
        /*
        public static Parser<T2> Select<T1, T2>(
            this Parser<T1> parser,
            Func<T1, T2> selector)
        */

        // Pointed Parser
        //
        // Pure(f).App(p) == p.Select(f)
        //
        public static Parser<T> Pure<T>(
            T value)
        {
            return Create((text, offset) =>
                new ParserResult<T>(value, text, offset, 0)
            );
        }

        // The following specializaitons of Pure allow for shorter type signatures
        public static Parser<Func<T1, TResult>> Lift<T1, TResult>(Func<T1, TResult> f) => Pure(f);
        public static Parser<Func<T1, Func<T2, TResult>>> Lift<T1, T2, TResult>(Func<T1, Func<T2, TResult>> f) => Pure(f);
        public static Parser<Func<T1, Func<T2, Func<T3, TResult>>>> Lift<T1, T2, T3, TResult>(Func<T1, Func<T2, Func<T3, TResult>>> f) => Pure(f);

        // Applicative Parser
        //
        // (the pointed parser could be subsumed by adding a projector function argument here)
        //
        // Since parsers are effectful, application is lazy only with respect to composition of 
        // parsers. It short ciruits on failure in order to support recursive parser application. 
        //
        // Application of the parse result is not lazy. The result of the first parser is applied to the result
        // of the second parser using strict call-by-value semantics. Thus, the second parser is evaluated as soon as 
        // the first parser succeeds and even if the result of the first parser doesn't use it's argument.
        // This behavior is consistent with the fact that parsers are effectful and may be executed
        // only because of their side effects. The Alternative and Monad instances of parsers provide
        // control over the execution of parsers.
        //
        public static Parser<T2> LazyApp<T1,T2>(
            this Parser<Func<T1,T2>> selector,
            Lazy<Parser<T1>> lazyParser)
        {
            return Create((text, offset) => 
                {
                    var res1 = selector.Parse(text, offset);
                    if (res1.IsMissing) return ParserResult<T2>.Missing;
                    var res2 = lazyParser.Value.Parse(text, offset + res1.Length);
                    if (res2.IsMissing) return ParserResult<T2>.Missing;
                    return new ParserResult<T2>(res1.Value(res2.Value), text, offset, res1.Length + res2.Length);
                });
        }

        // Since join is a primitive in LINQ, we provide a primitive
        // implementation here, even though it could be defined in terms of App.
        //
        // .NET offers a version of Join, with additional key selector functions, which constrain
        // the result so that it could possibly be empty. This allows us to implement failure
        // (where clause), and the identity (empty) element of an Alternative instance. It may
        // also allow us to implement an Alternative instance (TODO).
        //
        // The third function argument can be used to implement Pure.
        //
        public static Parser<TResult> Join<TOuter, TInner, TResult>(
            this Parser<TOuter> outer,
            Parser<TInner> inner,
            Func<TOuter, TInner, TResult> f)
        {
            return Create((text, offset) =>
                {   
                    var res1 = outer.Parse(text, offset);
                    if (res1.IsMissing) return ParserResult<TResult>.Missing;
                    var res2 = inner.Parse(text, offset + res1.Length);
                    if (res2.IsMissing) return ParserResult<TResult>.Missing;
                    return new ParserResult<TResult>(f(res1.Value,res2.Value), text, offset, res1.Length + res2.Length);
                }
            );
        }

        // Backtracking Alternative Parser
        //
        // TODO: it may be possible to implement this in terms of a Join with
        // key selector functions.
        //
        public static Parser<T> Or<T>(
            this Parser<T> parser1,
            Parser<T> parser2)
        {
            return Create((text, offset) => 
                {
                    var res1 = parser1.Parse(text, offset);
                    if (! res1.IsMissing) return new ParserResult<T>(res1.Value, res1.Source, res1.Position, res1.Length);
                    return parser2.Parse(text, offset);
                });
        }

        // Identity of Or. In haskell this is called empty.
        //
        // Where can be implemented in terms of Fail and both, where and fail, can be implement
        // in terms of a Join operator that supports key selection.
        //
        public static Parser<T> Fail<T>()
        {
            return Create((_text,_offset) => ParserResult<T>.Missing);
        }

        /* ****************************************************************** */
        /* Utils for parsing IEnumerable */

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> tail, T head)
        {
            yield return head;
            foreach (var i in tail) yield return i;
        }

        public static Func<T, Func<IEnumerable<T>, IEnumerable<T>>> Cons<T>() 
            => head => tail => tail.Prepend(head);
        public static Parser<Func<T, Func<IEnumerable<T>, IEnumerable<T>>>> ConsP<T>() 
            => Lift<T, IEnumerable<T>, IEnumerable<T>>(Cons<T>());

        public static IEnumerable<T> Nil<T>() => Enumerable.Empty<T>();
        public static Parser<IEnumerable<T>> NilP<T>() => Pure(Nil<T>());
        
        // Lazy right fold
        public static Parser<T2> FoldrP<T1,T2>(
            this Parser<T1> parser,
            T2 initial,
            Func<T1, Func<T2, T2>> f)
        {
            return Pure(f)
                .App(parser)
                .LazyApp(new Lazy<Parser<T2>>(() => parser.FoldrP(initial, f)))
                .Or(Pure(initial));
        }

        /* ****************************************************************** */
        /* Parsers */

        // strict call-by-value application
        public static Parser<T2> App<T1, T2>(
            this Parser<Func<T1, T2>> selector,
            Parser<T1> parser)
            => selector.LazyApp(new Lazy<Parser<T1>>(() => parser));

        // Ingore result of first parser
        public static Parser<T> Proj2<Ignore, T>(
            this Parser<Ignore> ignore,
            Parser<T> parser)
        {
            return Lift<Ignore,T,T>(_ => x => x)
                .App(ignore)
                .App(parser);
        }

        // Ingore result of second parser
        public static Parser<T> Proj1<T, Ignore>(
            this Parser<T> parser,
            Parser<Ignore> ignore)
        {
            return Lift<T,Ignore,T>(x => _ => x)
                .App(parser)
                .App(ignore);
        }

        public static Parser<T> Between<T, Ignore1, Ignore2>(
            this Parser<T> parser,
            Parser<Ignore1> before,
            Parser<Ignore2> after)
        {
            return before.Proj2(parser.Proj1(after));
        }

        public static Parser<IEnumerable<T>> Many<T>(
            this Parser<T> parser)
        {
            return FoldrP(parser, Enumerable.Empty<T>(), Cons<T>());
        }

        // The result is actully non-empty
        public static Parser<IEnumerable<T>> Some<T>(
            this Parser<T> parser)
        {
            return ConsP<T>()
                .App(parser)
                .App(parser.Many());
        }

        public static Parser<Tuple<IEnumerable<T1>,T2>> Until<T1,T2>(
            this Parser<T1> parser,
            Parser<T2> terminator)
        {
            var t = terminator.Select(t1 => Tuple.Create(Nil<T1>(), t1));
            var p = Lift<T1, Tuple<IEnumerable<T1>,T2>, Tuple<IEnumerable<T1>,T2>>(head => t1 => Tuple.Create(t1.Item1.Prepend(head), t1.Item2))
                .App(parser)
                .LazyApp(new Lazy<Parser<Tuple<IEnumerable<T1>,T2>>>(() => parser.Until(terminator)));
            return t.Or(p);
        }

        /* ... doesn't work, for instance, when used within Until, because this parser isn't pure.
         * I think we are here hitting the boundaries of what is still feasible from a practical
         * point of view within the limits of C#s type system.
         * In order to make this kind of things work in a managable way we would need a type system 
         * that supports better control of effects.
         *
        public static Parser<IEnumerable<T>> ParseN<T>(this Parser<T> parser, long n)
        {
            long i = n;
            return parser.Until(Pure(0).Where(_ => i-- == 0)).Select(t => t.Item1);
        }
        */

        public static Parser<IEnumerable<T>> ParseN<T>(this Parser<T> parser, long n)
        {
            if (n == 0) return NilP<T>();
            return ConsP<T>()
                .App(parser)
                // lazyness isn't needed here, but is more efficient for large values of n
                .LazyApp(new Lazy<Parser<IEnumerable<T>>>(() => parser.ParseN(n - 1)));
        }

        public static Parser<IEnumerable<T>> separatedBy<T,S>(
            this Parser<T> parser,
            Parser<S> separator)
        {
            return ConsP<T>()
                .App(parser)
                .App(separator.Proj2(parser).Many())
                .Or(NilP<T>());
        }

        public static Parser<IEnumerable<T>> separatedByN<T,S>(
            this Parser<T> parser,
            Parser<S> separator,
            long n)
        {
            if (n == 0) return NilP<T>();
            if (n == 1) return Lift<T, IEnumerable<T>>(head => new[] { head }).App(parser);
            return ConsP<T>()
                .App(parser.Proj1(separator))
                // lazyness isn't needed here, but is more efficient for large values of n
                .LazyApp(new Lazy<Parser<IEnumerable<T>>>(() => parser.separatedByN(separator, n - 1)));
        }
    }
}
