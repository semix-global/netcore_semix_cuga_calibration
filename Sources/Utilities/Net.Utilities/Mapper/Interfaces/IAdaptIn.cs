namespace Net.Utilities.Mapper.Interfaces;

public interface IAdaptIn<in TIn, out TOut>
{
    TOut AdaptIn(TIn obj);
}