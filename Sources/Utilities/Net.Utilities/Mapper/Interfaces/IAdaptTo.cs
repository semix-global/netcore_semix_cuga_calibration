namespace Net.Utilities.Mapper.Interfaces;

public interface IAdaptTo<out T>
{
    T AdaptTo();
}