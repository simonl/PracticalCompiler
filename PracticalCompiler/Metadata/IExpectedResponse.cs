namespace PracticalCompiler.Metadata
{
    public interface IExpectedResponse<M>
    {
        Expected<Response> OnTag { get; }

        Option<Unit> OnError { get; }
        Option<M> OnResult { get; }
    }

    public sealed class ExpectedResponse<M> : IExpectedResponse<M>
    {
        public Expected<Response> OnTag { get; private set; }
        public Option<Unit> OnError { get; private set; } 
        public Option<M> OnResult { get; private set; }

        public ExpectedResponse(Expected<Response> onTag, Option<Unit> onError, Option<M> onResult)
        {
            OnTag = onTag;
            OnError = onError;
            OnResult = onResult;
        }
    }
}