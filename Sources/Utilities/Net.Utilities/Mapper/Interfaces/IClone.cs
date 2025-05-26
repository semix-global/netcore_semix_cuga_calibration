namespace Net.Utilities.Mapper.Interfaces;

public interface ICloneable<out T>
{
    T Clone();
}