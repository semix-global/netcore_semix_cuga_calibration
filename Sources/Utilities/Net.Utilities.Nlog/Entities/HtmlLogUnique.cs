namespace Net.Utilities.Nlog.Entities;

public enum HtmlLogUniqueTypeEnum
{
    Logging,
    LoggingPeek,
    LoggingClear,
    LoggedEnd
}

public readonly struct HtmlLogUnique
{
    public Guid Guid { get; }

    public HtmlLogUniqueTypeEnum HtmlLogUniqueTypeEnum { get; }

    public string FileName { get; }

    internal HtmlLogUnique(Guid guid, HtmlLogUniqueTypeEnum htmlLogUniqueTypeEnum, string fileName = "")
    {
        Guid = guid;
        HtmlLogUniqueTypeEnum = htmlLogUniqueTypeEnum;
        FileName = fileName;
    }
}