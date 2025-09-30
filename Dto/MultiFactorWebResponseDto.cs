namespace MultiFactor.IIS.Adapter.Dto
{
    public class MultiFactorWebResponseDto<TModel>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public TModel Model { get; set; }
    }
}